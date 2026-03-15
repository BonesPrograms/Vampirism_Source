using System;
using VampirismSys.Core;

namespace XRL.World.Effects
{

    [Serializable]


    public class Pale : Dazed
    {

        int victimHP => base.Object.GetHPPercent();
        bool victim => base.Object.HasEffectDescendedFrom<BaseFeedEffect>();
        public Pale()
        {
            Duration = 9999;
            DisplayName = "{{Y sequence|pale}}";
        }
        public override bool Apply(GameObject Object)
        {
            if (base.Object.IsPlayer())
                AddPlayerMessage("Your skin turns {{Y sequence|pale}}.");
            else
                AddPlayerMessage(base.Object.t() + " looks {{Y sequence|pale}}.");
         return base.Apply(Object);
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
            if (WikiRng.Next(1, 50) == 1)
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
                        base.Object.ApplyEffect(new Woozy(5));
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