using System;
using XRL.World.Capabilities;
using Nexus.Core;

namespace XRL.World.Effects
{

    [Serializable]
    class KO : Asleep
    {
        public int victimHP => base.Object.GetHPPercent();
        public bool victim => base.Object.HasEffectDescendedFrom<IFeeding>();
        public KO() => DisplayName = "unconscious";

        public override bool SameAs(Effect e) => false;
        public KO(int Duration)
            : this()
        {
            base.Duration = Duration;
        }
        public override bool HandleEvent(IsConversationallyResponsiveEvent E)
        {
            if (E.Speaker == base.Object)
            {
                if (E.Mental && !E.Physical)
                {
                    E.Message = base.Object.Poss("mind") + " is in disarray.";
                }
                else
                {
                    E.Message = base.Object.Does("can't") + " respond to you.";
                }

                return false;
            }

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetCompanionStatusEvent E)
        {
            if (E.Object == base.Object)
                E.AddStatus("unconscious", 100);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            BloodRegen();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetInventoryActionsEvent E) => true;

        public override bool Apply(GameObject Object)
        {
            if (Object.HasEffect<Woozy>())
                Object.RemoveEffect<Woozy>();
            Object.ApplyEffect(new Prone(LyingOn: AsleepOn, Voluntary: false));
            Object.MovementModeChanged("Asleep", !Voluntary);
            if (Object.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You pass out from bloodloss!");
            else if (Visible())
                IComponent<GameObject>.AddPlayerMessage(Object.t() + " passes out from bloodloss.");
            Object.Brain?.Goals.Clear();
            Object.ForfeitTurn();
            if (Object.IsPlayer())
                AutoAct.Interrupt();
            ApplyStats();
            return true;
        }
        public override void Remove(GameObject Object)
        {
            UnapplyStats();
            if (!victim && base.Object.HasHitpoints())
            {
                if (base.Object.IsPlayer())
                    AddPlayerMessage("You shamble to your feet.");
                else
                    AddPlayerMessage(base.Object.t() + " shambles to " + base.Object.its + " feet.");
                DidX("wake", "up in a daze", null, null, null, null, base.Object);
                base.Object.ApplyEffect(new Dazed(WikiRng.Next(3, 5), false));
                base.Object.ApplyEffect(new Woozy(9999, 5));
            }
        }

        void ApplyStats() => base.StatShifter.SetStatShift("DV", -12);
        void UnapplyStats() => base.StatShifter.RemoveStatShifts(base.Object);
        void BloodRegen()
        {
            if (!victim)
                if (victimHP >= 25)
                {
                    Duration = 0;
                }
        }
    }
}
