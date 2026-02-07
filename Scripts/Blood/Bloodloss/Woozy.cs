using System;

namespace XRL.World.Effects
{
    [Serializable]
    class Woozy : Disoriented
    {
        public int victimHP => base.Object.GetHPPercent();
        public bool victim => base.Object.HasEffectDescendedFrom<IFeeding>();
        public Woozy() => DisplayName = "{{g sequence|woozy}}";
        public override bool Apply(GameObject Object)
        {
            if (base.Object.IsPlayer())
                AddPlayerMessage("You feel {{g sequence|woozy}}.");
            else if (!base.Object.HasEffect<Asleep>())
                AddPlayerMessage(base.Object.t() + " looks {{g sequence|woozy}}.");
            return base.Apply(Object);
        }

        public override void Remove(GameObject Object)
        {
            if (!victim && base.Object.HasHitpoints())
            {
                if (base.Object.IsPlayer())
                    AddPlayerMessage("You feel better.");
                else if (!base.Object.HasEffect<Asleep>())
                    AddPlayerMessage(base.Object.t() + " feels better.");
            }
            base.Remove(Object);
        }
        public Woozy(int Duration, int Level)
            : this()
        {
            base.Duration = Duration;
            this.Level = Level;
        }

        public override bool SameAs(Effect e) => false;

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID)
                return true;
            return base.WantEvent(ID,cascade);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            BloodRegen();
            return base.HandleEvent(E);
        }

        void BloodRegen()
        {
            if (!victim)
                if (victimHP >= 75)
                    Duration = 0;
        }
    }
}