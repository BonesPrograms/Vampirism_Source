using XRL.World;
using System.Collections.Generic;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using System.Linq;

namespace Nexus.Frenzy
{

    /// <summary>
    /// Controls assignment and modification of the keys in TargetRegistry.
    /// </summary>
    public class Search
    {
        readonly public TheBeast Source;
        public Search(TheBeast Source) => this.Source = Source;
        public bool TryScan(out GameObject Object)
        {
            RegisterTargets();
            Object = Source.TargetRegistry.Any(x => x.Value != TheBeast.FLAG_AVOID) ? Select() : null;
            return Object is not null;
        }

        GameObject Select()
        {
            if (Source.TargetRegistry.Count > 1)
            {
                int minimum = Source.TargetRegistry.Values.Min();
                return Source.TargetRegistry.First(x => x.Value == minimum).Key;
            }
            else
                return Source.TargetRegistry.ElementAt(0).Key;
        }
        bool LightCheck(GameObject tgt, int distance)
        {
            if (tgt.CurrentCell.GetLight() == LightLevel.None)
            {
                if (Source.ParentObject.TryGetPart(out HeightenedHearing HH) && distance <= HH.GetRadius())
                    return true;
                if (Source.ParentObject.TryGetPart(out HeightenedSmell HS) && distance <= HS.GetRadius())
                    return true;
                return false;
            }
            else
                return true;
        }
        bool ValidForRegistration(GameObject target)
         =>
            !Source.TargetRegistry.ContainsKey(target)
            && target != Source.ParentObject
            && target?.CurrentCell?.GetCombatTarget(Source.ParentObject) is not null
            && Source.ParentObject.HasLOSTo(target, IncludeSolid: false)
            && Source.ParentObject.canPathTo(target.CurrentCell)
            && Core.Checks.Applicable(target)
            && target.InSameZone(Source.ParentObject) //noticed a bug in early testing where you would run off the map to targets in nearbyzones if this wasnt here 
            && !target.IsFlying //though its been so long im not sure if i was just doing an improper Clean()
            && LightCheck(target, Source.ParentObject.DistanceTo(target));

        void RegisterTargets()
        {
            List<GameObject> targets = Source.ParentObject.CurrentZone.GetObjectsWithTagOrProperty("Bleeds");
            for (int i = 0; i < targets.Count; i++)
                if (ValidForRegistration(targets[i]))
                    Source.TargetRegistry.Add(targets[i], targets[i].DistanceTo(Source.ParentObject));
        }


    }
}