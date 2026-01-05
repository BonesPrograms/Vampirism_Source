using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL;
using System.Linq;

namespace Nexus.Biting
{

    /// <summary>
    /// Frontend for the bite simulator mechanics behind Biting - evaluates targets and creates BiteSimulator instance if bad target = true.
    /// </summary>
    class Bite : VampireBite
    {
        public bool IsOnFire { get; private set; }
        public bool HasPlasma { get; private set; }
        public bool HasBadLiquid => BadLiquids.ContainsValue(true);
        public bool HasDisease => Diseases.ContainsValue(true);
        public bool IsPoisoned => Poisons.ContainsValue(true);
        public Bite(GameObject Biter, GameObject Target) : base(Biter, Target)
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

        bool VomitEnding(Vampirism Vampirism)
        {
            Vampirism.BiteActivate(Target);
            Blood.Overrides.Vomit(Biter);
            Target.AddOpinion<OpinionDominate>(Biter);
            return true;
        }

        bool Fail(Vampirism Vampirism)
        {
            Vampirism.BiteActivate(Target);
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

        /// <summary>
        /// Should not run if BadTarget returns false, otherwise you will get OutOfRange().
        /// </summary>
        /// <returns></returns>
        public bool CannotFeed() => new BiteSimulator(Biter, Target, this).BadEnding() switch
        {
            Ending.VOMIT => VomitEnding(Biter.GetPart<Vampirism>()),
            Ending.FAIL => Fail(Biter.GetPart<Vampirism>()),
            Ending.SUCCESS => Success(),
            Ending.PAIN_TOLERANCE => PainTolerance(),
            _ => OutOfRange(),
        };

        /// <summary>
        /// Method for evaluating object state and gathering data for later use in CannotFeed();
        /// </summary>
        public bool BadTarget()
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
            List<Type> diseases = new(Diseases.Keys);
            foreach (Type disease in diseases)
                Diseases[disease] = Target.HasEffect(disease);
        }
        void PoisonCheck()
        {
            List<Type> poisons = new(Poisons.Keys);
            foreach (Type poison in poisons)
                Poisons[poison] = Target.HasEffect(poison);
            if (Target.TryGetEffect(out StingerPoisoned e) && e.Owner == Biter)
                Poisons[typeof(StingerPoisoned)] = false;
        }

        void LiquidCheck()
        {
            if (Target.TryGetEffect(out LiquidCovered L))
            {
                List<string> keys = new(BadLiquids.Keys);
                foreach (string liquid in keys)
                    BadLiquids[liquid] = L.Liquid.ContainsLiquid(liquid);
                if (Biter.HasEffect<Blaze_Tonic>())
                    BadLiquids["asphalt"] = false;

            }
        }




    }
}