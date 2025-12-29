using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL;
using System.Linq;

namespace Nexus.Bite
{

    /// <summary>
    /// Frontend for the bite simulator mechanics behind Biting - evaluates targets and creates BiteSimulator instance if bad target = true.
    /// </summary>
    class Biting : VampireBite
    {
        public bool IsOnFire { get; private set; }
        public bool HasPlasma { get; private set; }
        public bool HasBadLiquid => BadLiquids.ContainsValue(true);
        public bool HasDisease => Diseases.ContainsValue(true);
        public bool IsPoisoned => Poisons.ContainsValue(true);
        Biting(GameObject Biter, GameObject Target) : base(Biter, Target)
        {
        }

        readonly public Dictionary<string, bool> BadLiquids = new()
        {
          {"sludge", false},
          {"ooze", false},
          {"goo", false},
          {"oil", false},
          {"acid", false},
          {"slime", false},
          {"putrid", false},
          {"asphalt", false}
        };

        readonly public Dictionary<Type, bool> Diseases = new()
        {
                {typeof(Glotrot), false},
                {typeof(GlotrotOnset), false},
                {typeof(Ironshank), false},
                {typeof(IronshankOnset), false}
        };

        readonly public Dictionary<Type, bool> Poisons = new()
        {
                {typeof(Poisoned), false},
                {typeof(StingerPoisoned), false},
                {typeof(PoisonGasPoison), false}
        };

        /// <summary>
        /// Finalizes and determines the level of success or failure of the attack, after data on the target has been gathered, evaulated, and results have been returned.
        /// </summary>


        bool VomitEnding()
        {
            Biter.GetPart<Vampirism>().BiteActivate(Target);
            Blood.Helpers.Vomit(Biter);
            Target.AddOpinion<OpinionDominate>(Biter);
            return true;
        }

        bool Fail()
        {
            Biter.GetPart<Vampirism>().BiteActivate(Target);
            if (Biter.IsPlayer())
                Popup.ShowFail("You reel away from " + Target.t() + "!");
            Target.AddOpinion<OpinionDominate>(Biter);
            return true;
        }

        bool Success()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("...but you feed anyways!");
            return false;
        }

        new bool PainTolerance()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("You feel no pain and feed on " + Target.t() + "!");
            return false;
        }

        bool OutOfRange()
        {
            MetricsManager.LogModError(ModManager.GetMod("vampirism"), "BiteSimulator.BadEnding() returned out of range value in Biting.CannotFeed(). Possible empty list in BadEnding() Possible error in Biting.TryCreateInstance() (should automatically set booleans to prevent access of empty lists).");
            return true;
        }
        public bool CannotFeed() => new BiteSimulator(Biter, this).BadEnding() switch
        {
            Endings.VOMIT => VomitEnding(),
            Endings.FAIL => Fail(),
            Endings.SUCCESS => Success(),
            Endings.PAIN_TOLERANCE => PainTolerance(),
            _ => OutOfRange(),
        };

        /// <summary>
        /// To prevent accidental misuse of method order. Target is evaluated upon instancing.
        /// </summary>
        public static bool TryCreateInstance(GameObject Biter, GameObject Victim, out Biting Bite)
        {
            Bite = new(Biter, Victim);
            Bite = Bite.BadTarget() ? Bite : null;
            return Bite is not null;
        }


        /// <summary>
        /// Method for evaluating object state and gathering data for later use in CannotFeed();
        /// </summary>
        bool BadTarget()
        {
            DiseaseCheck();
            PoisonCheck();
            LiquidCheck();
            IsOnFire = Target.IsAflame() && !Biter.HasEffect<Blaze_Tonic>();
            HasPlasma = Target.HasEffect<CoatedInPlasma>();
            return IsOnFire || HasPlasma || IsPoisoned || HasDisease || HasBadLiquid;
        }
        void DiseaseCheck()
        {
            foreach (Type disease in Diseases.Keys.ToList())
                if (Target.HasEffect(disease))
                    Diseases[disease] = true;
        }
        void PoisonCheck()
        {
            foreach (Type poison in Poisons.Keys.ToList())
                if (Target.HasEffect(poison))
                    Poisons[poison] = true;
            if (Target.TryGetEffect(out StingerPoisoned e) && e.Owner == Biter)
                Poisons[typeof(StingerPoisoned)] = false; //this allows you to consume your own poison from victims, though wont matter if other poisons are present in their body
        }
        void LiquidCheck()
        {
            if (Target.TryGetEffect(out LiquidCovered L))
            {
                foreach (string liquid in BadLiquids.Keys.ToList())
                    if (L.Liquid.ContainsLiquid(liquid))
                        BadLiquids[liquid] = true;
                if (BadLiquids["asphalt"] && Biter.HasEffect<Blaze_Tonic>())
                    BadLiquids["asphalt"] = false;
            }
        }




    }
}