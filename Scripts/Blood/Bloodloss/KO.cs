using System;
using XRL.World.Capabilities;
using VampirismSys.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using VampirismSys.Core;

namespace XRL.World.Effects
{

    [Serializable]
    public class KO : Asleep
    {
        int victimHP => base.Object.GetHPPercent();
        bool victim => base.Object.HasEffectDescendedFrom<BaseFeedEffect>();
        public override bool SameAs(Effect e) => false;
        public KO()
        {
            DisplayName = "unconscious";
            base.Duration = 9999;
        }

        public KO(int Duration, bool forced) : this()
        {
            base.Duration = Duration;
            this.forced = forced;
            quicksleep = false;
            Voluntary = false;
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
            if (Object.HasEffectDescendedFrom<Asleep>())
                return false;
            Object.RemoveEffect<Woozy>();
            Object.ApplyEffect(new Prone(LyingOn: AsleepOn, Voluntary: false));
            Object.MovementModeChanged("Asleep", !Voluntary);
            if (Object.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You pass out from bloodloss!");
            else if (Visible())
                IComponent<GameObject>.AddPlayerMessage(Object.t() + " passes out from bloodloss.");
            Object.Brain.Goals.Clear();
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
                base.Object.ApplyEffect(new Woozy(5));
            }
        }

        private void ApplyStats() //instead of instancing asleep and calling these with reflection, we copy
        {
            base.StatShifter.SetStatShift("DV", -12);
        }

        private void UnapplyStats()
        {
            base.StatShifter.RemoveStatShifts(base.Object);
        }

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
