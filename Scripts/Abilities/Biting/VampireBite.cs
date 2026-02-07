using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core;

namespace Nexus.Biting
{

    /// <summary>
    /// Minimum value takes priority.
    /// </summary>
    /// 
    /// 
    public enum Ending
    {
        VOMIT = 4,
        FAIL = 3,
        PAIN_TOLERANCE = 2,
        SUCCESS = 1,
        OUT_OF_RANGE = 0
    }

    public abstract class VampireBite
    {
        readonly public GameObject Biter;
        protected VampireBite(GameObject Biter) => this.Biter = Biter;
        protected bool PainTolerance() => Biter.HasEffect<HulkHoney_Tonic>() || Biter.HasPart<Analgesia>();
        protected Ending MakeSave(string text) => Biter.MakeSave("Toughness", 13, null, null, text) ? Ending.SUCCESS : Ending.FAIL;
        protected Ending Result(Ending[] endresults) => endresults.Length > 0 ? endresults.Max() : Ending.OUT_OF_RANGE;
        //default enum value is 0. ever since we switched to arrays we use Max() instead. higher numbers take priority and indicate failure.
        //thus if your Max() value is 0 you know something has gone wrong and it wasnt just a normal failure

    }
}
