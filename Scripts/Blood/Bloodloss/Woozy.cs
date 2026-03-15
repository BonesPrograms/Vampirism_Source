using System;

namespace XRL.World.Effects
{
    [Serializable]
    public class Woozy : Disoriented
    {
        int victimHP => base.Object.GetHPPercent();
        bool victim => base.Object.HasEffectDescendedFrom<BaseFeedEffect>();
       public Woozy()
        {
            DisplayName = "{{g|woozy}}";
            Duration = 9999;
        }
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
        internal Woozy(int Level) : this()
        {
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