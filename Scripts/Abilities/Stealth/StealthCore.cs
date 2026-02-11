using XRL.World;
using System.Collections.Generic;
using XRL.World.Parts.Mutation;
using Nexus.Core;
using XRL.World.Effects;
using System.Linq;
using XRL.World.Parts;

namespace Nexus.Stealth
{
    /// <summary>
    /// Scans the environment and constantly updates the lists used in Nightbeast.
    /// </summary>
    public class StealthCore
    {
        readonly Nightbeast Source;
        public LightLevel? LightLevel;
        public StealthCore(Nightbeast Source) => this.Source = Source;
        public StealthCore(Nightbeast Source, LightLevel? LightLevel)
        {
            this.Source = Source;
            this.LightLevel = LightLevel;
        }

        /// <summary>
        /// The evaluation that separates a NearbySentient from an ActiveWitness. It checks if they are aware and if they can see you based on light levels.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool ActiveWitness(GameObject obj)
        {
            return !obj.Unaware(false) && !Shrouded(obj);
        }

        /// <summary>
        /// The evaluation that separates a NearbySentient from a ValidSentient. It restricts by AI RADIUS and LOS.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public bool NearbySentient(GameObject witness)
        {
            return witness.HasLOSTo(Source.ParentObject, false) && witness.DistanceTo(Source.ParentObject) <= Nexus.Rules.STEALTH.AI_RADIUS;
        }

        /// <summary>
        /// The evaluation that is mostly for security and keeps friendlies, dead people, yourself (if dominating), and objects outside the zone off the list.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public bool ValidSentient(GameObject witness)
          =>
            witness != null
            && CheckParts(witness.PartsList)
            && !Inanimate(witness)
            && !witness.IsFriendly(Source.ParentObject)
            && witness.HasHitpoints()
            && witness.CurrentZone == Source.Zone
            && CheckEffect(witness.Effects);

        /// <summary>
        /// It is recommended to exclude plants from your lists of witnesses (you'll see me do it often in Alert and Spotter), because being spotted by vines, roots and
        /// ivories felt strange.
        /// </summary>
        /// 

        public static bool CheckEffect(XRL.Collections.Rack<Effect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                System.Type type = effects[i].GetType();
                if (type == typeof(Dominated) || type == typeof(Dominating))
                    return false;
            }
            return true;
        }
        public static bool Inanimate(GameObject witness)
         =>
             witness.Body?.Anatomy == "Echinoid"
            || CheckTags(witness.GetBlueprint());

        static bool CheckTags(GameObjectBlueprint Blueprint)
        {

            foreach (var data in Blueprint.Tags)
            {
                if (data.Key == "Culture" && (data.Value == "Plant" || data.Value == "Fungal"))
                    return true;
                else if (data.Key == "Class" && (data.Value == "fungus" || data.Value == "root"))
                    return true;
                else if (data.Key == "Species" && data.Value == "root")
                    return true;
                else
                    return CheckKey(data.Key);
            }
            return false;
        }

        static bool CheckKey(string key)
        {
            return key == "LivePlant" || key == "Plank" || key == "HangingSupport" || key == "LiveFungus" || key == "ExcludeFromHostiles";
        }

        static bool CheckParts(PartRack rack)
        {
            for (int i = 0; i < rack.Count; i++)
            {
                System.Type Type = rack[i].GetType();
                if (Type == typeof(Harvestable) || Type == typeof(PlantProperties) || Type == typeof(FungusProperties))
                    return false;
                else if (Type == typeof(Brain))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Simple method that evaluates if you are detectable via lighting. Light levels in a cell are relative to what the player can see only,
        ///  and if you are using nightvision, your light level is technically not dark. This method considers those extra possibilities to ensure everything works.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>

        public bool Shrouded(GameObject witness)
         =>
            LightLevel switch
            {
                XRL.World.LightLevel.None or XRL.World.LightLevel.Darkvision or XRL.World.LightLevel.Dimvision
                => !SpottedByDarkvision(witness, Source.ParentObject.DistanceTo(witness)), //shrouded must return true, but if SpottedByDarkvision is true, then we have to return false
                null => BadLight(),
                _ => false
            };

        bool BadLight()
        {
            string err = "Err @ StealthCore.Shrouded()";
            string message = Source.ParentObject?.CurrentCell is null ? $"{err} : current cell is null, lightlevel null" : $"{err} : attempting to access Shrouded() without assigning light level!";
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), message);
            return false;
        }

        bool SpottedByDarkvision(GameObject witness, int DistanceTo)
        {
            // if (witness.TryGetPart(out DarkVision D) && DistanceTo <= D.Radius)
            //     return true;
            if (witness.TryGetPart(out HeightenedSmell HS) && DistanceTo <= HS.GetRadius())
                return true;
            if (witness.TryGetPart(out HeightenedHearing HH) && DistanceTo <= HH.GetRadius())
                return true;
            //    if (witness.TryGetPart(out XRL.World.Parts.Mutation.NightVision N) && distance <= N.Level * 5)
            //   return true; // suspeneded until i can figure out how the actual range for nightvision works
            return false;
        }

        /// <summary>
        /// This method isn't really for you, it is for the main stealth part. It is not advised to invoke this yourself.
        /// </summary>
        public void ScanEnvironment()
        {
            for (int y = 0; y < Source.Zone.Height; y++)
            {
                for (int x = 0; x < Source.Zone.Width; x++)
                {
                    Cell cell = Source.Zone.Map[y][x];
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        GameObject obj = cell.Objects[i];
                        CheckValidity(obj);
                    }
                }
            }
        }

        void CheckValidity(GameObject obj)
        {
            if (ValidSentient(obj) && NearbySentient(obj) && ActiveWitness(obj))
                Source.Witnesses.Add(obj);
            else
                Source.Witnesses.Remove(obj);
        }
    }
}

