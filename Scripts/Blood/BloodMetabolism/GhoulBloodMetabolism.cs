using System;
using XRL.World.Effects;
using VampirismSys.Blood;
using VampirismSys.Core;

namespace XRL.World.Parts
{
    [Serializable]
    public class GhoulBloodMetabolism : BaseBloodMetabolism
    {
        public static readonly string[] Stats = { "Strength", "Agility", "Toughness", "Willpower", "Ego", "Hitpoints" };

        public override int MetabolismRate => VampirismSys.Rules.Vitae.Metab_Settings.DEFAULT / 2;

        public bool Buffed;

        public bool Bloodstarved;

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
            EnthralledGhoul e = ParentObject.GetEffect<EnthralledGhoul>();
            if (Status < BloodLevel.QUENCHED)
                SetBloodStarved(e);
            else if (Bloodstarved)
                RemoveBloodStarved(e);
        }

        void SetBloodStarved(EnthralledGhoul e)
        {
            if (!Bloodstarved)
            {
                e.DisplayName = "{{r|bloodstarved}}";
                Bloodstarved = true;
            }
            IComponent<GameObject>.AddPlayerMessage($"{ParentObject.t()} feels " + "{{R|thirsty}}.");
        }

        void RemoveBloodStarved(EnthralledGhoul e)
        {
            e.DisplayName = "{{r|ghoul}}";
            Bloodstarved = false;
            Stats.ForEach(x => StatShifter.RemoveStatShift(ParentObject, x));
        }

    }
}
