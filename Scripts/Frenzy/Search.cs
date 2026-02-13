using XRL.World;
using System.Collections.Generic;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using System.Linq;
using Nexus.Core;

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
            Sift();
            Register();
            int min = Source.TargetRegistry.Values.Min();
            Object = Source.TargetRegistry.AnyIsnt(TheBeast.FLAG_AVOID) ? Source.TargetRegistry.PickFirst(min).Key : null;
            return Object != null;
        }

        void Sift()
        {
            foreach (var obj in Source.TargetRegistry.KeyArray())
            {
                if (!obj?.HasHitpoints() ?? true) //serious bug here (OR DOWN IN THE BADKEY CHECK IN REGISTER()) (they were doing the same evaluation)
                    Source.TargetRegistry.Remove(obj); //it was prematurely removing objects due to one of these two checks : !InSameZone or Target.CurrentCell.CombatTarget(ParentObject) == null so if that object was a badtarget you would get softlocked attacking them over and over
            }                                           // i realized we dont really need either of them so its fine
        }                                                //if anyone ends up biting a phase spider wrongly, i will fix it then
                                                         //this caused too much headache for me to worry about right now I FIXED IT

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
            return true;
        }

        public void Register()
        {
            Zone zone = Source.ParentObject.CurrentZone;
            for (int y = 0; y < zone.Height; y++)
            {
                for (int x = 0; x < zone.Width; x++)
                {
                    Cell cell = zone.Map[x][y];
                    Register(cell);
                }
            }
        }

        public void Register(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                GameObject obj = cell.Objects[i];
                if (BadKey(obj))
                {
                    if (!obj?.HasHitpoints() ?? true)
                        Source.TargetRegistry.Remove(obj);
                    continue;
                }

                if (ValidForRegistration(obj))
                {
                    Source.TargetRegistry[obj] = obj.DistanceTo(Source.ParentObject);
                }
                else
                    Source.TargetRegistry.Remove(obj);
            }
        }
        public bool BadKey(GameObject Actor)
        {
            Source.TargetRegistry.TryGetValue(Actor, out int value);
            return value == TheBeast.FLAG_AVOID;
        }
        public bool ValidForRegistration(GameObject target)
         =>
            target != Source.ParentObject
            && target != null
            && target.CurrentCell?.GetCombatTarget(Source.ParentObject) != null
            && target.InSameZone(Source.ParentObject) //noticed a bug in early testing where you would run off the map to targets in nearbyzones if this wasnt here 
            && !target.IsFlying //though its been so long im not sure if i was just doing an improper Clean()
            && target.HasTagOrProperty("Bleeds")
            && target.HasHitpoints()
            && Source.ParentObject.HasLOSTo(target, IncludeSolid: false)
            && Source.ParentObject.canPathTo(target.CurrentCell)
            && target.IsVisible()
            && Core.Checks.Applicable(target)
            && LightCheck(target, Source.ParentObject.DistanceTo(target));
    }
}