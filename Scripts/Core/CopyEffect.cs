using XRL.World.Effects;
using XRL.World;
using System;
using System.Collections.Generic;
using XRL.World.Parts;

namespace Nexus.Core
{



    //example method

    // virtual T Copy(T e)
    // {
    //     T CopiedEffect = new()
    //     {
    //         Duration = e.Duration
    //     };
    //     return CopiedEffect; //create and return a new object with the same field values as the object from the parameter
    // }
    interface ICopyEffect<T> where T : Effect
    {
        public T Copy(T e);
    }
    //this class was specifically designed for ApplyEffectToWitnesses
    //makes it so that you can pass an effect instance as a parameter, but each witness gets a new, cloned version of the effect
    //instead of all sharing a reference to the same effect

    //not totally necessary, you could just apply effects to the people in the list on your own
    //but i thought this would be a fun project

    /// <summary>
    /// This class ensures that objects are not sharing a reference to the same effect instance by making manual copies and new instances.
    /// </summary>
    static class CopyEffect
    {
        // public static List<T> Verify<T>(List<GameObject> objects) where T : Effect, new()
        // {
        //     List<T> count = new();
        //     foreach (GameObject obj in objects)
        //     {
        //         if (obj.TryGetEffect(out T e) && !count.Contains(e))
        //         {
        //             count.Add(e);
        //             //    if (e is Beguiled beguiled)
        //             //  cmd.msg($"{beguiled?.Beguiler}");
        //         }
        //     }
        //     // cmd.msg($"{count.Count}");
        //     foreach (T obj in count)
        //     {
        //         //  cmd.msg($"{obj}, {obj?.ID}");
        //     }
        //     return count;
        // }
        static bool Isnull()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Warning in Alert.AddEffectToWitnesses<T> : CopyEffect.TryCopy<T> was provided a null instance of T Effect");
            return false;
        }

        static bool Checksupport<T>(T obj) where T : Effect
        {
            if (obj is null)
                MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Warning in Alert.AddEffectToWitnesses<T> : CopyEffect does not support the provided effect. Implement from ICopyEffect<T> in your effect to provide support.");
            return obj is not null;
        }

        public static bool TryCopy<T>(T Effect, out T obj) where T : Effect // recommended to use this one because it has fail state notifications
        {
            obj = null;
            if (Effect is not null)
            {
                obj = Copy(Effect);
                return Checksupport(obj);
            }
            else
                return Isnull();
        }

        public static T Copy<T>(T Effect) where T : Effect //supports many but not all basegame effects and mod effects that implement ICopyEffect<T>
         =>
             Effect switch
             {
                 ICopyEffect<T> e => Copy(e),
                 Stun e => (T)(Effect)Copy(e),
                 Berserk e => (T)(Effect)Copy(e),
                 Asleep e => (T)(Effect)Copy(e),
                 Frenzied e => (T)(Effect)Copy(e),
                 Dazed e => (T)(Effect)Copy(e),
                 Lovesick e => (T)(Effect)Copy(e),
                 Bleeding e => (T)(Effect)Copy(e),
                 Blind e => (T)(Effect)Copy(e),
                 Burning e => (T)(Effect)Copy(e),
                 Disoriented e => (T)(Effect)Copy(e),
                 Confused e => (T)(Effect)Copy(e),
                 Budding e => (T)(Effect)Copy(e),
                 Beguiled e => (T)(Effect)Copy(e),
                 AshPoison e => (T)(Effect)Copy(e),
                 BlinkingTicSickness e => (T)(Effect)Copy(e),
                 CoatedInPlasma e => (T)(Effect)Copy(e),
                 LiquidCovered e => (T)(Effect)Copy(e),
                 _ => null
             };

        public static T Copy<T>(ICopyEffect<T> e) where T : Effect
        {
            return e.Copy((T)e);
        }
        
        /// <summary>
        /// Using LiquidCovered can be a bit funky - be careful and test.
        /// </summary>
        public static LiquidCovered Copy(LiquidCovered e)
        {
            LiquidCovered obj = new()
            {
                Liquid = e.Liquid,
                Poured = e.Poured,
              //  PouredBy = e.PouredBy,
              //  FromCell = e.FromCell,
            };
            return obj;
        }
        public static CoatedInPlasma Copy(CoatedInPlasma e)
        {
            CoatedInPlasma obj = new()
            {
                Duration = e.Duration,
                Owner = e.Owner
            };
            return obj;
        }

        public static BlinkingTicSickness Copy(BlinkingTicSickness e)
        {
            BlinkingTicSickness obj = new()
            {
                Duration = e.Duration
            };
            return obj;
        }

        public static AshPoison Copy(AshPoison e)
        {
            AshPoison obj = new()
            {
                Duration = e.Duration,
                Damage = e.Damage,
                Owner = e.Owner
            };
            return obj;
        }

        public static Beguiled Copy(Beguiled e)
        {
            Beguiled obj = new()
            {
                Beguiler = e.Beguiler,
                Level = e.Level,
                LevelApplied = e.LevelApplied,
                Independent = e.Independent
            };
            return obj;
        }

        public static Budding Copy(Budding e)
        {
            Budding obj = new()
            {
                numClones = e.numClones,
                baseDuration = e.baseDuration,
                Duration = e.Duration,
                ActorID = e.ActorID,
                ReplicationContext = e.ReplicationContext

            };
            return obj;
        }

        public static Confused Copy(Confused e)
        {       
            Confused obj = new()
            {
                Duration = e.Duration,
                Level = e.Level,
                MentalPenalty = e.MentalPenalty,
                NameForChecks = e.NameForChecks
            };
            return obj;
        }
        public static Disoriented Copy(Disoriented e)
        {
            Disoriented obj = new()
            {
                Duration = e.Duration,
                Level = e.Level
            };
            return obj;
        }

        public static Burning Copy(Burning e)
        {
            Burning obj = new()
            {
                Duration = e.Duration
            };
            return obj;
        }

        public static Blind Copy(Blind e)
        {
            Blind obj = new()
            {
                Duration = e.Duration
            };
            return obj;
        }
        public static Bleeding Copy(Bleeding e)
        {
            Bleeding obj = new()
            {
                Duration = e.Duration,
                Damage = e.Damage,
                SaveTarget = e.SaveTarget,
                Owner = e.Owner,
                Stack = e.Stack,
                Internal = e.Internal,
                StartMessageUsePopup = e.StartMessageUsePopup,
                StopMessageUsePopup = e.StopMessageUsePopup,
                Bandaged = e.Bandaged
            };
            return obj;
        }

        public static Berserk Copy(Berserk e)
        {
            Berserk obj = new()
            {
                Duration = e.Duration
            };
            return obj;
        }

        public static Asleep Copy(Asleep e)
        {
            Asleep obj = new()
            {
                Duration = e.Duration,
                forced = e.forced,
                quicksleep = e.quicksleep,
                Voluntary = e.Voluntary,
            };
            return obj;
        }

        public static Frenzied Copy(Frenzied e)
        {
            Frenzied obj = new()
            {
                QuicknessBonus = e.QuicknessBonus,
                MaxKillRadiusBonus = e.MaxKillRadiusBonus,
                BerserkDuration = e.BerserkDuration,
                BerserkImmediately = e.BerserkImmediately,
                BerserkOnDealDamage = e.BerserkOnDealDamage,
                PreferBleedingTarget = e.PreferBleedingTarget,
                Duration = e.Duration
            };
            return obj;
        }
        public static Stun Copy(Stun e)
        {
            Stun obj = new()
            {
                Duration = e.Duration,
                DVPenalty = e.DVPenalty,
                SaveTarget = e.SaveTarget,
                DontStunIfPlayer = e.DontStunIfPlayer,
            };
            return obj;
        }

        public static Dazed Copy(Dazed e)
        {
            Dazed obj = new()
            {
                Duration = e.Duration,
                DontStunIfPlayer = e.DontStunIfPlayer,
                Penalty = e.Penalty,
                SpeedPenalty = e.SpeedPenalty
            };
            return obj;

        }

        public static Lovesick Copy(Lovesick e)
        {
            Lovesick obj = new()
            {
                Beauty = e.Beauty,
                Duration = e.Duration
            };
            return obj;
        }
    }
}