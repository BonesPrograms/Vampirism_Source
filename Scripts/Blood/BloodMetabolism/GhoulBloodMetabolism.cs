using System;
using XRL.World.Effects;
using VampirismSys.Blood;

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
            AddPlayerMessage($"{ParentObject.t()} is starving for " + "{{r|blood}}!");
            int debuff = DebuffRate;
            foreach (var obj in GhoulBloodMetabolism.Stats)
            {
                StatShifter.SetStatShift(obj, debuff);
            }
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
            foreach (var obj in Stats)
                StatShifter.RemoveStatShift(ParentObject, obj);
        }

    }
}
