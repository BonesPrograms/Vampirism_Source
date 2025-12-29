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
        readonly List<GameObject> Exclusions;
        readonly List<GameObject> Additions;
        readonly bool ConsiderPlantsWitnesses;
        readonly bool ForceStealth;

        /// <summary>
        /// For use with methods that don't require access to local fields.
        /// </summary>
        public StealthCore()
        {
        }
        public StealthCore(Nightbeast Source, LightLevel? LightLevel, bool ConsiderPlantsWitnesses, List<GameObject> Exclusions, List<GameObject> Additions, bool ForceStealth)
        {
            this.Source = Source;
            this.LightLevel = LightLevel;
            this.ConsiderPlantsWitnesses = ConsiderPlantsWitnesses;
            this.ForceStealth = ForceStealth;
            this.Exclusions = Exclusions;
            this.Additions = Additions;
        }

        public void ScanEnvironment()
        {
            if (Source is not null)
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
            else
                MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error@ StealthCore.ScanEnviroment(): Source is null. Must use proper constructor if trying to access ScanEnvironment.");
        }


        void GetValidSentients()
        {
            foreach (GameObject witness in Source.Sentients)
                if (ValidSentient(witness) && !Source.ValidSentients.Contains(witness))
                    Source.ValidSentients.Add(witness);
        }

        void GetNearbySentientsInLOS()
        {
            foreach (var witness in Source.ValidSentients)
            { //if you want to select between one of these two options, you'll have to make your own list and apply it to Additions
                if (witness.HasLOSTo(Source.ParentObject, false) && witness.DistanceTo(Source.ParentObject) <= Source.AI_RADIUS && !Source.NearbySentients.Contains(witness))
                    Source.NearbySentients.Add(witness);
            }
        }
        void GetActiveWitnesses()
        {

            foreach (GameObject witness in Source.NearbySentients)
                if (!Source.ActiveWitnesses.Contains(witness) && !Scan.Unaware(witness, false))
                {
                    if (Exclusions?.Contains(witness) ?? false)
                        continue;
                    if (Plant(witness) && !ConsiderPlantsWitnesses)
                        continue;
                    if (Shrouded(witness))
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

        public bool ValidSentient(GameObject witness)
          =>
            witness != Source.ParentObject
            && Source.ValidSentients.Contains(witness) == false
            && Scan.IsFriendly(witness, Source.ParentObject) == false
            && witness.HasHitpoints()
            && witness.InSameZone(Source.ParentObject)
            && witness.HasEffect<Dominating>() == false;

        public bool Plant(GameObject witness)
         =>
            witness.GetSpecies() == "root"
            || witness.HasTagOrProperty("root")
            || witness.HasTagOrProperty("LivePlant")
            || witness.HasTagOrProperty("Plant") //im awware this excludes all plants even sentients but i found it really annoying being spotted by inanimate plants in caves
            || witness.HasTagOrProperty("Plank")
            || witness.HasPart<Harvestable>()
            || witness.HasTagOrProperty("ExcludeFromHostiles")
            || witness.HasTagOrProperty("HangingSupport");

        public bool Shrouded(GameObject witness)
         =>
            LightLevel switch
            {
                XRL.World.LightLevel.None or XRL.World.LightLevel.Darkvision or XRL.World.LightLevel.Dimvision
                => SpottedByDarkvision(witness, Source.ParentObject.DistanceTo(witness)) == false, //shrouded must return true, but if SpottedByDarkvision is true, then we have to return false
                null => BadLight(),
                _ => false
            };

        bool BadLight()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ StealthCore.Shrouded() -- Player current cell is null, unable to get light level.");
            return false;
        }

        public bool SpottedByDarkvision(GameObject witness, int DistanceTo)
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
            foreach (GameObject obj in List.ToList())
            {
                if (obj?.CurrentCell is null || obj == Source.ParentObject || !obj.InSameZone(Source.ParentObject) || !obj.HasHitpoints() || !obj.HasLOSTo(Source.ParentObject, false) || Scan.IsFriendly(obj, Source.ParentObject) || obj.HasEffect<Dominating>())
                    List.Remove(obj);
                else if ((NearbySentients || ActiveWitnesses) && obj.DistanceTo(Source.ParentObject) > Source.AI_RADIUS)
                    List.Remove(obj);
                else if (ActiveWitnesses && (Shrouded(obj) || Scan.Unaware(obj, false)) || !ConsiderPlantsWitnesses && Plant(obj)) //else if prevents us from attempting to evaluate null objects in these two methods
                    List.Remove(obj);                                                   //unaware and SpottedByDarkvision are not prepared for null objects
            }
        }


    }
}
