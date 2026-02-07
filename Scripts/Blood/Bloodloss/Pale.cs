using System;
using Nexus.Core;

namespace XRL.World.Effects
{

    [Serializable]


    class Pale : Dazed
    {

        public int victimHP => base.Object.GetHPPercent();
        public bool victim => base.Object.HasEffectDescendedFrom<IFeeding>();
        public Pale()
        {
            DisplayName = "{{Y sequence|pale}}";
        }

        public Pale(int Duration)
            : this()
        {
            base.Duration = Duration;
        }

        public override bool Apply(GameObject Object)
        {
            if (base.Object.IsPlayer())
                AddPlayerMessage("Your skin turns {{Y sequence|pale}}.");
            else
                AddPlayerMessage(base.Object.t() + " looks {{Y sequence|pale}}.");
            if (Object.Brain is null)
            {
                return false;
            }

            if (Object.HasEffect<Dazed>())
            {
                if (!DontStunIfPlayer || !Object.IsPlayer() || !Object.HasEffect<Stun>())
                {
                    Object.ApplyEffect(new Stun(1, 30, DontStunIfPlayer));
                }

                return false;
            }

            if (!Object.FireEvent(Event.New("ApplyDazed", "Duration", Duration)))
            {
                return false;
            }
            if (!Object.HasEffect<Asleep>())
                DidX("are", "dazed", null, null, null, null, Object);
            Object.ParticleText("*dazed*", IComponent<GameObject>.ConsequentialColorChar(null, Object));
            ApplyStats();
            return true;
        }

        private void ApplyStats()
        {
            Penalty = 4;
            SpeedPenalty = 10;
            base.StatShifter.SetStatShift(base.Object, "Intelligence", -Penalty);
            base.StatShifter.SetStatShift(base.Object, "Agility", -Penalty);
            base.StatShifter.SetStatShift(base.Object, "MoveSpeed", SpeedPenalty);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID)
            {
                return true;
            }
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            BloodRegen();
            if (WikiRng.Next(1, 50) is 1)
                base.Object.ApplyEffect(new Prone(false, false, false));
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Object)
        {
            if (!victim && base.Object.HasHitpoints())
            {
                if (base.Object.IsPlayer())
                    AddPlayerMessage("The color returns to your skin.");
                else
                    AddPlayerMessage("The color returns to " + base.Object.t() + "'s skin.");
            }
            base.Remove(Object);
        }
        void BloodRegen()
        {
            if (!victim)
            {
                if (victimHP >= 50)
                {
                    if (!base.Object.HasEffect<Woozy>() && !base.Object.HasEffect<Asleep>())
                        base.Object.ApplyEffect(new Woozy(999, 5));
                    Duration = 0;
                }
            }
        }
        public override bool SameAs(Effect e)
        {
            return false;
        }
    }
}