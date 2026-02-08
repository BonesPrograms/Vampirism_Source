using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL;
using Nexus.Core;
using System.Linq;

namespace Nexus.Biting
{

    /// <summary>
    /// Frontend for the bite simulator mechanics behind Biting - evaluates targets and creates BiteSimulator instance if bad target = true.
    /// </summary>
    public class Bite : VampireBite
    {
        public bool IsOnFire { get; private set; }
        public bool HasPlasma { get; private set; }
        public bool HasBadLiquid => BadLiquids.ContainsValue(true);
        public bool HasDisease => Diseases.ContainsValue(true);
        public bool IsPoisoned => Poisons.ContainsValue(true);
        readonly Vampirism Vampirism;
        readonly BiteSimulator Sim;
        public Bite(GameObject Biter, Vampirism Vampirism) : base(Biter)
        {
            this.Vampirism = Vampirism;
            Sim = new(Biter, this);
        }
        public bool[] Flags => new bool[]
        {
            IsOnFire, HasPlasma, HasBadLiquid, HasDisease, IsPoisoned
        };
        readonly public (string, bool)[] BadLiquids =
        {
          ("sludge", false),
          ("ooze", false),
          ("goo", false),
          ("oil", false),
          ("acid", false),
          ("slime", false),
          ("putrid", false),
          ("asphalt", false)
        };

        readonly public (Type, bool)[] Diseases =
        {
                (typeof(Glotrot), false),
                (typeof(GlotrotOnset), false),
                (typeof(Ironshank), false),
                (typeof(IronshankOnset), false)
        };

        readonly public (Type, bool)[] Poisons =
        {
                (typeof(Poisoned), false),
                (typeof(StingerPoisoned), false),
                (typeof(PoisonGasPoison), false)
        };

        bool VomitEnding(GameObject Target)
        {
            Fail(Target);
            Blood.Overrides.Vomit(Biter);
            return true;
        }

        bool Fail(GameObject Target)
        {
            Vampirism.BiteActivate(Target);
            if (Biter != null && Target != null)
            {
                if (Biter.IsPlayer())
                    Popup.ShowFail("You reel away from " + Target.t() + "!");
                else if (Target.IsPlayer())
                    IComponent<GameObject>.AddPlayerMessage($"{Biter.t()} reels away from you!");
                else
                    IComponent<GameObject>.AddPlayerMessage($"{Biter.t()} reels away from {Target.t()}!");
                Target.AddOpinion<OpinionDominate>(Biter);
            }
            return true;
        }

        bool Success()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("...but you feed anyways!");
            return false;
        }

        bool PainTolerance(GameObject Target)
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
        public bool CannotFeed(GameObject Target) => Sim.BadEnding(Target) switch
        {
            Ending.VOMIT => VomitEnding(Target),
            Ending.FAIL => Fail(Target),
            Ending.SUCCESS => Success(),
            Ending.PAIN_TOLERANCE => PainTolerance(Target),
            _ => OutOfRange(),
        };

        /// <summary>
        /// Method for evaluating object state and gathering data for later use in CannotFeed();
        /// </summary>
        public bool BadTarget(GameObject Target)
        {
            CheckDictionaries(Target);
            IsOnFire = Target.IsAflame();
            FlameProof();
            HasPlasma = Target.HasEffect<CoatedInPlasma>();
            return IsOnFire || HasPlasma || IsPoisoned || HasDisease || HasBadLiquid;
        }

        void FlameProof()
        {
            if (Biter.HasEffect<Blaze_Tonic>())
            {
                IsOnFire = false;
                BadLiquids[7].Item2 = false;
            }
        }

        void DiseaseCheck(GameObject Target) => CheckEffects(Target, Diseases);
        void PoisonCheck(GameObject Target)
        {
            CheckEffects(Target, Poisons);
            if (Target.TryGetEffect<StingerPoisoned>(out var e) && e.Owner == Biter)
                Poisons[1].Item2 = false;
        }

        void LiquidCheck(GameObject Target)
        {
            if (Target.TryGetEffect(out LiquidCovered L))
            {
                for (int i = 0; i < BadLiquids.Length; i++)
                    BadLiquids[i].Item2 = L.Liquid.ContainsLiquid(BadLiquids[i].Item1);
            }
            else
                BadLiquids.Reset(); // only one that needs manual resetting
                //other arrays are always scanned every bite

        }

        void CheckDictionaries(GameObject Target)
        {
            DiseaseCheck(Target);
            PoisonCheck(Target);
            LiquidCheck(Target);
        }
        static void CheckEffects(GameObject Target, (Type, bool)[] source)
        {
            for (int i = 0; i < source.Length; i++)
                source[i].Item2 = Target.HasEffect(source[i].Item1);
        }

    }
}