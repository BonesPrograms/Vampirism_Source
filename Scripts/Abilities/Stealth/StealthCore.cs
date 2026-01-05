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
    class StealthCore
    {
        readonly Nightbeast Source;
        readonly LightLevel? LightLevel;
        public List<GameObject> Exclusions;
        public List<GameObject> Additions;
        public bool ConsiderPlantsWitnesses;
        readonly bool ForceStealth;
        public StealthCore(Nightbeast Source) => this.Source = Source;
        public StealthCore(Nightbeast Source, LightLevel? LightLevel, bool ConsiderPlantsWitnesses = false, List<GameObject> Exclusions = null, List<GameObject> Additions = null, bool ForceStealth = false)
        {
            this.Source = Source;
            this.LightLevel = LightLevel;
            this.ConsiderPlantsWitnesses = ConsiderPlantsWitnesses;
            this.ForceStealth = ForceStealth;
            this.Exclusions = Exclusions;
            this.Additions = Additions;
        }

        /// <summary>
        /// The evaluation that separates a NearbySentient from an ActiveWitness. It checks if they are aware and if they can see you based on light levels.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool ActiveWitness(GameObject obj) => !Scan.Unaware(obj, false) && !Shrouded(obj);

        /// <summary>
        /// The evaluation that separates a NearbySentient from a ValidSentient. It restricts by AI RADIUS and LOS.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public bool NearbySentient(GameObject witness) => witness.HasLOSTo(Source.ParentObject, false) && witness.DistanceTo(Source.ParentObject) <= Source.AI_RADIUS;

        /// <summary>
        /// The evaluation that is mostly for security and keeps friendlies, dead people, yourself (if dominating), and objects outside the zone off the list.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public bool ValidSentient(GameObject witness)
          =>
            !Scan.IsFriendly(witness, Source.ParentObject)
            && witness.HasHitpoints()
            && witness.InSameZone(Source.ParentObject)
            && !witness.HasEffect<Dominating>()
            && !witness.HasEffect<Dominated>();

        /// <summary>
        /// It is recommended to exclude plants from your lists of witnesses (you'll see me do it often in Alert and Spotter), because being spotted by vines, roots and
        /// ivories felt strange.
        /// </summary>
        public static bool Inanimate(GameObject witness)
         =>
             witness.HasTagOrProperty("root")
            || witness.HasTagOrProperty("LivePlant")
            || witness.HasTagOrProperty("Plant") //im awware this excludes all plants even sentients but i found it really annoying being spotted by inanimate plants in caves
            || witness.HasTagOrProperty("Plank")
            || witness.HasTagOrProperty("ExcludeFromHostiles")
            || witness.HasTagOrProperty("HangingSupport")
            || witness.HasTagOrProperty("Fungus")
            || witness.HasTagOrProperty("LiveFungus")
            || witness.GetSpecies() == "root"
            || witness.Body.Anatomy == "Echinoid"
            || witness.HasPart<Harvestable>()
            || witness.HasPart<PlantProperties>()
            || witness.HasPart<FungusProperties>();

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
        void GetValidSentients()
        {
            foreach (GameObject witness in Source.Sentients)
                if (!Source.ValidSentients.Contains(witness) && ValidSentient(witness) && witness != Source.ParentObject)
                    Source.ValidSentients.Add(witness);
        }
        void GetNearbySentientsInLOS()
        {
            foreach (GameObject witness in Source.ValidSentients)
            { //if you want to select between one of these two options, you'll have to make your own list and apply it to Additions
                if (!Source.NearbySentients.Contains(witness) && NearbySentient(witness))
                    Source.NearbySentients.Add(witness);
            }
        }
        void GetActiveWitnesses()
        {

            foreach (GameObject witness in Source.NearbySentients)
                if (!Source.ActiveWitnesses.Contains(witness) && ActiveWitness(witness))
                {
                    if (Exclusions?.Contains(witness) ?? false)
                        continue;
                    if (StealthCore.Inanimate(witness) && !ConsiderPlantsWitnesses)
                        continue;
                    else
                        Source.ActiveWitnesses.Add(witness);
                }
            if (Additions is not null)
            {
                foreach (var obj in Additions)
                {
                    if (!Source.ActiveWitnesses.Contains(obj))
                        Source.ActiveWitnesses.Add(obj);
                }
            }

        }

        bool BadLight()
        {
            string err = "Err @ StealthCore.Shrouded()";
            string message = Source.ParentObject?.CurrentCell is null ? $"{err} : current cell is null, lightlevel null" : $"{err} : attempting to access Shrouded() without assigning light level!";
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), message);
            return false;
        }

        bool SpottedByDarkvision(GameObject witness, int DistanceTo)
        {
            if (witness.TryGetPart(out DarkVision D) && DistanceTo <= D.Radius)
                return true;
            if (witness.TryGetPart(out HeightenedSmell HS) && DistanceTo <= HS.GetRadius())
                return true;
            if (witness.TryGetPart(out HeightenedHearing HH) && DistanceTo <= HH.GetRadius())
                return true;
            //    if (witness.TryGetPart(out XRL.World.Parts.Mutation.NightVision N) && distance <= N.Level * 5)
            //   return true; // suspeneded until i can figure out how the actual range for nightvision works
            return false;
        }
        void Clean(List<GameObject> List, bool ActiveWitnesses, bool NearbySentients)
        {
            List<GameObject> objects = new(List);
            foreach (GameObject obj in objects)
            {
                if (obj?.CurrentCell is null || !ValidSentient(obj))
                    List.Remove(obj);
                else if ((NearbySentients || ActiveWitnesses) && !NearbySentient(obj))
                    List.Remove(obj);
                else if (ActiveWitnesses && (!ActiveWitness(obj) || (!ConsiderPlantsWitnesses && Inanimate(obj)))) //else if prevents us from attempting to evaluate null objects in these two methods
                    List.Remove(obj);                                                   //unaware and SpottedByDarkvision are not prepared for null objects
            }
        }

        /// <summary>
        /// This method isn't really for you, it is for the main stealth part. It is not advised to invoke this yourself.
        /// </summary>
        public void ScanEnvironment()
        {
            Clean(Source.ValidSentients, false, false);
            Clean(Source.NearbySentients, false, true);
            Clean(Source.ActiveWitnesses, true, false);
            GetValidSentients();
            GetNearbySentientsInLOS();
            if (ForceStealth)
                Source.ActiveWitnesses.Clear();
            else
            {
                GetActiveWitnesses();
                // if (Exclusions is not null)
                // {
                //     foreach (var obj in Exclusions)
                //         if (Source.ActiveWitnesses.Contains(obj))
                //             Source.ActiveWitnesses.Remove(obj);
                // }
                // if (Additions is not null)
                // {
                //     foreach (var obj in Additions)
                //         Source.ActiveWitnesses.Add(obj);
                // }
            }
        }

    }
}
