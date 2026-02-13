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
            return !obj.Unaware(false) && !Shrouded(obj) && !obj.IsFriendly(Source.ParentObject) && obj.HasHitpoints() && CheckEffect(obj.Effects);
        }

        /// <summary>
        /// The evaluation that separates a NearbySentient from a ValidSentient. It restricts by AI RADIUS and LOS.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public bool NearbySentient(GameObject witness)
        {
            return witness.HasLOSTo(Source.ParentObject, false) && witness.DistanceTo(Source.ParentObject) <= Nexus.Rules.STEALTH.AI_RADIUS && witness.InSameZone(Source.ParentObject);
        }

        /// <summary>
        /// The evaluation that is mostly for security and keeps friendlies, dead people, yourself (if dominating), and objects outside the zone off the list.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public static bool ValidSentient(GameObject witness)
          =>
            witness?.Brain != null
            && witness.IsCombatObject()
            && !Inanimate(witness);
        public static bool Inanimate(GameObject witness)
     =>
         witness.Body?.Anatomy == "Echinoid"
        || CheckTags(witness.GetBlueprint())
        || CheckParts(witness.PartsList);

        /// <summary>
        /// It is recommended to exclude plants from your lists of witnesses (you'll see me do it often in Alert and Spotter), because being spotted by vines, roots and
        /// ivories felt strange.
        /// </summary>
        /// 

        static bool CheckEffect(XRL.Collections.Rack<Effect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                System.Type type = effects[i].GetType();
                if (type == typeof(Dominated) || type == typeof(Dominating))
                    return false;
            }
            return true;
        }

        static bool CheckTags(GameObjectBlueprint Blueprint)
        {

            if (Blueprint?.Tags != null)
            {
                foreach (var data in Blueprint.Tags)
                {
                    if (CheckPair(data))
                        return true;
                }
            }
            return false;
        }

        static bool CheckPair(KeyValuePair<string, string> data)
        {
            if (data.Key == "Culture" && (data.Value == "Plant" || data.Value == "Fungal"))
                return true;
            if (data.Key == "Class" && (data.Value == "fungus" || data.Value == "root"))
                return true;
            if (data.Key == "Species" && data.Value == "root")
                return true;
            if (CheckKey(data.Key))
                return true;
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
                if (Type == typeof(Harvestable) || Type == typeof(PlantProperties) || Type == typeof(FungusProperties) || Type == typeof(Harvestable))
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

        public void Sift()
        {
            foreach (var obj in Source.Witnesses.KeyArray())
            {
                if (!obj?.HasHitpoints() ?? true)
                    Source.Witnesses.Remove(obj); //stealth system re-checks the zone every single turn after loading and will change flags of objects based on the two bool methods
            }                                       //however tests with frenzy showed us that dead objects will remain dormant in the citionary, so we need to sift our dictionary as well
        }                                           //though we do not need to worry about samezone like we do there because we recreate the dictionary on zoneload
        public void ScanEnvironment()
        {
            Sift();
            for (int y = 0; y < Source.Zone.Height; y++)
            {
                for (int x = 0; x < Source.Zone.Width; x++)
                {
                    Cell cell = Source.Zone.Map[x][y];
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        CheckValidity(cell.Objects[i]);
                    }
                }
            }
        }

        void CheckValidity(GameObject obj)
        {
            if (obj.TryGetStringProperty(Properties.FLAGS.VALID, out var result) && result == Properties.FLAGS.TRUE)
            {
                bool Check = NearbySentient(obj) && ActiveWitness(obj);
                Source.Witnesses[obj] = Check;
                if (Check)
                    Source.TrueCount++;
            }
        }
    }
}

