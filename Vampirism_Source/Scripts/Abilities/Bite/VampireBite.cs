using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using System.Collections.Generic;
using System;

namespace Nexus.Bite
{

    /// <summary>
    /// Minimum value takes priority.
    /// </summary>
    /// 
    enum Endings
    {
        VOMIT,
        FAIL,
        SUCCESS,
        PAIN_TOLERANCE,
        OUT_OF_RANGE
    }
    abstract class VampireBite // for shared methods and fields that most VampireBite inheritors need
    {
        readonly public GameObject Biter;
        readonly public GameObject Target;
        public VampireBite(GameObject Biter) => this.Biter = Biter;
        public VampireBite(GameObject Biter, GameObject Target)
        {
            this.Biter = Biter;
            this.Target = Target;
        }
        protected bool PainTolerance() => Biter.HasEffect<HulkHoney_Tonic>() || Biter.HasPart<Analgesia>();
        protected Endings MakeSave(string text) => Biter.MakeSave("Toughness", 13, null, null, text) ? Endings.SUCCESS : Endings.FAIL;
    }
}