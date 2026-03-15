using System;
using VampirismSys.Blood;
using VampirismSys.Core;
using XRL.World.Effects;

namespace XRL.World.Parts
{
    [Serializable]
    public class GhoulBloodMetabolism : BaseBloodMetabolism
    {
        public static readonly string[] Stats = { "Strength", "Agility", "Toughness", "Willpower", "Ego", "Hitpoints" };

        protected override int MetabolismRate => VampirismSys.Rules.Metab.Metab_Settings.DEFAULT / 2;

        bool Buffed;

        bool Bloodstarved;

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == EffectRemovedEvent.ID)
                return Buffed;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect.GetType() == typeof(BuffedEnthralledGhoul))
                Buffed = false;
            return base.HandleEvent(E);
        }

        int DebuffRate => Status switch //rough draft, in the future the scaling will be based on the individual value of each statistic
        {                               //though this will probably be handled at the site of debuff, and DebuffRate will be used as a "base debuff value"
            BloodLevel.THIRSTY => 4,
            BloodLevel.PARCHED => 8,
            BloodLevel.MIN => 12,
            _ => default
        };
        protected override void Cycle()
        {
            if (StatusChange(out var LostBlood, out _))
            {
                CheckStatus();
                if (Bloodstarved && LostBlood)
                    Debuff();
            }
            if (Blood <= 0)
                ParentObject.Die(); //just like that
            else
                base.Cycle();
        }

        public void Buff(int Roll)
        {
            if (!Buffed && Status > BloodLevel.THIRSTY) //they need to be fed a bit before buff can kick in
            {
                ParentObject.ApplyEffect(new BuffedEnthralledGhoul(Roll));
                Buffed = true;
            }
            Drink();
        }

        void Debuff()
        {
            string msg = Status > BloodLevel.MIN ? $"{ParentObject.t()} is thirsty for " + "{{r|blood}}!" : $"{ParentObject.t()} will die without " + "{{r|blood}}!";
            AddPlayerMessage(msg);
            int debuff = DebuffRate;
            Stats.ForEach(x => StatShifter.SetStatShift(x, debuff));
        }

        void CheckStatus()
        {
            IGhoulEffect e = ParentObject.GetEffectDescendedFrom<IGhoulEffect>();
            if (Status < BloodLevel.QUENCHED)
                SetBloodStarved(e);
            else if (Bloodstarved)
                RemoveBloodStarved(e);
        }

        void SetBloodStarved(IGhoulEffect e)
        {
            if (!Bloodstarved)
            {
                e.Name = "{{r|bloodstarved}}";
                Bloodstarved = true;
            }
            IComponent<GameObject>.AddPlayerMessage($"{ParentObject.t()} feels " + "{{R|thirsty}}.");
        }

        void RemoveBloodStarved(IGhoulEffect e)
        {
            e.Name = e.Thrall ? "{{r|ghoul}}" : "{{r|relinquished}}";
            Bloodstarved = false;
            Stats.ForEach(x => StatShifter.RemoveStatShift(ParentObject, x));
        }

    }
}
