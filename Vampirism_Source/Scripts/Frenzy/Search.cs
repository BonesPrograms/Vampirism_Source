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
            GameObject obj = null;
            if (Source.TargetRegistry.Count == 1)
                obj = Source.TargetRegistry.ElementAt(0).Key;
            else if (Source.TargetRegistry.Count != 0)
            {
                int minimum = Source.TargetRegistry.Values.Min();
                obj = Source.TargetRegistry.First(x => x.Value == minimum).Key;
            }
            return obj;

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
            && Core.Scan.Applicable(target)
            && target.InSameZone(Source.ParentObject)
            && !target.IsFlying
            && LightCheck(target, Source.ParentObject.DistanceTo(target));

        void RegisterTargets()
        {
            foreach (GameObject target in Source.ParentObject.CurrentZone.GetObjectsWithTagOrProperty("Bleeds"))  //this combo where HasLOSTo IncludeSolid: false && beast.ParentObject.canPathTo are what allow the player to frenzy while using a forcefield, but simultaneously, not frenzy if an enemy is using a forcefield. without thsee two pieces, then you will frenzy against unreachable (but visible) enemies who are behind forcefields and get softlocked, or not frenzy at all just because you have a forcefield, thus exploiting and avoiding the frenzy system entirely
                if (ValidForRegistration(target))
                    Source.TargetRegistry.Add(target, target.DistanceTo(Source.ParentObject));

        }


    }
}