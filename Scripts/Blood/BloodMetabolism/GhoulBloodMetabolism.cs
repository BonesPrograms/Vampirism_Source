using System;
using VampirismSys.Blood;
using VampirismSys.Extensions;
using VampirismSys.Rules;
using XRL.Core;
using XRL.UI;
using XRL.World.Effects;

namespace XRL.World.Parts
{
    [Serializable]
    public class GhoulBloodMetabolism : BaseBloodMetabolism
    {

        public static bool ShowBlood;
        public static readonly string[] Stats = { "Strength", "Agility", "Toughness", "Willpower", "Ego", "Hitpoints" };

        protected override int MetabolismRate => 5;

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
            if (E.Effect is BuffedEnthralledGhoul)
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
            if (ShowBlood)
                AddPlayerMessage(Blood.ToString());
            if (Blood <= 0)
            {
                ParentObject.Die(); //just like that
            }
            else
            {
                base.Cycle();
                if (StatusChange(out var LostBlood, out _))
                {
                    CheckStatus();
                    if (Bloodstarved && LostBlood)
                    {
                        Capabilities.AutoAct.Interrupt();
                        Debuff();
                    }
                }
            }

        }

        public bool Feed(int roll)
        {
            if (Blood >= Metab.SIP_PUKE_WARN)
            {
                if (Popup.ShowYesNo($"Feeding {ParentObject.t()} will make them puke. Do you still want to feed {ParentObject.it}?") == DialogResult.No)
                    return false;
                Drink();
                return true;
            }
            if (Blood < Metab.BLOOD_QUENCHED)
                Blood = Metab.BLOOD_QUENCHED;
            if (Bloodstarved)
                RemoveBloodStarved(ParentObject.GetEffect<EnthralledGhoul>());
            Drink();
            ParentObject.FireEvent("Recuperating");
            //   RegenerateLimbEvent.Send(ParentObject, null, null, Whole: true);
            ParentObject.FireEvent(Event.New("Regenera", "SourceDescription", "The {{r|vampire blood}} cures you of", "Level", 1));
            if (!Buffed)
            {
                ParentObject.ApplyEffect(new BuffedEnthralledGhoul(roll));
                Buffed = true;
            }
            return true;
        }

        void Debuff()
        {
            ParentObject.RemoveEffect<BuffedEnthralledGhoul>();
            string msg = Status > BloodLevel.MIN ? $"{ParentObject.t()} is thirsty for " + "{{r|blood}}!" : $"{ParentObject.t()} will die without " + "{{r|blood}}!";
            AddPlayerMessage(msg);
            int debuff = DebuffRate;
            Stats.ForEach(x => StatShifter.SetStatShift(x, -debuff));
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
                e.Description = "{{r|bloodstarved}}";
                Bloodstarved = true;
            }
            // IComponent<GameObject>.AddPlayerMessage($"{ParentObject.t()} feels " + "{{R|thirsty}}.");
        }

        void RemoveBloodStarved(IGhoulEffect e)
        {
            e.Description = e.Thrall ? "{{r|ghoul}}" : "{{r|masterless}}";
            Bloodstarved = false;
            Stats.ForEach(x => StatShifter.RemoveStatShift(ParentObject, x));
            AddPlayerMessage($"{ParentObject.t()}'s" + " {{r|bloodthirst}} is quenched.");
        }

        public override bool Render(RenderEvent E)
        {
            int num = XRLCore.CurrentFrame % 60;
            if (Bloodstarved && num > 25 && num < 35)//XRLCore.CurrentFrame % 20 > 10)
            {
                E.RenderString = "\u0003";
                E.ColorString = "&r^k";
            }
            return true;
        }

    }
}
