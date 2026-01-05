using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Biting
{

    /// <summary>
    /// Minimum value takes priority.
    /// </summary>
    /// 
    /// 
    enum Ending
    {
        VOMIT,
        FAIL,
        SUCCESS,
        PAIN_TOLERANCE,
        OUT_OF_RANGE
    }

    abstract class VampireBite
    {
        readonly protected GameObject Biter;
        readonly protected GameObject Target;
        protected VampireBite(GameObject Biter) => this.Biter = Biter;
        protected VampireBite(GameObject Biter, GameObject Target)
        {
            this.Biter = Biter;
            this.Target = Target;
        }
        protected bool PainTolerance() => Biter.HasEffect<HulkHoney_Tonic>() || Biter.HasPart<Analgesia>();
        protected Ending MakeSave(string text) => Biter.MakeSave("Toughness", 13, null, null, text) ? Ending.SUCCESS : Ending.FAIL;
        protected Ending Result(List<Ending> endresults) => endresults.Count != 0 ? endresults.Min() : Ending.OUT_OF_RANGE;

    }
}
