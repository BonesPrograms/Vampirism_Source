using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using System.Linq;
using VampirismSys.Extensions;

namespace VampirismSys.Frenzy
{

    /// <summary>
    /// Controls assignment and modification of the keys in TargetRegistry.
    /// </summary>
    internal class Search
    {
        readonly TheBeast Source;
        internal Search(TheBeast Source)
        {
            this.Source = Source;
        }
        internal bool TryScan(out GameObject tgt)
        {
            tgt = null;
            Sift();
            Register();
            if (Source.TargetRegistry.Any(x => x.Value != TheBeast.FLAG_AVOID))
            {
                int min = Source.TargetRegistry.Values.Min();
                tgt = Source.TargetRegistry.First(x => x.Value == min).Key;
            }
            return tgt != null;
        }

        void Sift()
        {
            GameObject[] invalids = Source.TargetRegistry.Keys.Where(x => x == null || !x.HasHitpoints() || !x.InSameZone(Source.ParentObject)).ToArray();
            invalids.ForEach(x => Source.TargetRegistry.Remove(x));
        }



        //serious bug here (OR DOWN IN THE BADKEY CHECK IN REGISTER()) (they were doing the same evaluation)}
        //it was prematurely removing objects due to one of these two checks : !InSameZone or Target.CurrentCell.CombatTarget(ParentObject) == null so if that object was a badtarget you would get softlocked attacking them over and over
        // i realized we dont really need either of them so its fine
        //if anyone ends up biting a phase spider wrongly, i will fix it then
        //this caused too much headache for me to worry about right now I FIXED IT

        bool LightCheck(GameObject tgt, int distance)
        {
            if (tgt.CurrentCell.GetLight() == LightLevel.None || !tgt.IsVisible())
            {
                if (Source.ParentObject.TryGetPart(out HeightenedHearing HH) && distance <= HH.GetRadius())
                    return true;
                if (Source.ParentObject.TryGetPart(out HeightenedSmell HS) && distance <= HS.GetRadius())
                    return true;
                return false;
            }
            return true;
        }

        internal void Register()
         => Source.ParentObject.CurrentZone.CombatObjects().SafeForEach(registerDelegate);

        void registerDelegate(GameObject obj)
        {
            if (BadKey(obj))
            {
                if (!obj?.HasHitpoints() ?? true)
                    Source.TargetRegistry.Remove(obj);
            }
            else if (ValidForRegistration(obj))
            {
                Source.TargetRegistry[obj] = obj.DistanceTo(Source.ParentObject);
            }
            else
                Source.TargetRegistry.Remove(obj);
        }
        internal bool BadKey(GameObject Actor)
        {
            Source.TargetRegistry.TryGetValue(Actor, out int value);
            return value == TheBeast.FLAG_AVOID;
        }
        internal bool ValidForRegistration(GameObject target)
         =>
            target != Source.ParentObject
            && target != null
            && target.CurrentCell?.GetCombatTarget(Source.ParentObject) != null
            && target.InSameZone(Source.ParentObject) //noticed a bug in early testing where you would run off the map to targets in nearbyzones if this wasnt here 
            && target.HasHitpoints()
            && Source.ParentObject.HasLOSTo(target, IncludeSolid: false)
            && Source.ParentObject.canPathTo(target.CurrentCell)
            && Checks.AttackableForAI(target)
            && LightCheck(target, Source.ParentObject.DistanceTo(target));
    }
}