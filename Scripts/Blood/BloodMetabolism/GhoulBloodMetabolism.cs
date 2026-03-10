using XRL.World;
using Nexus.Core;
using XRL.UI;
using XRL.World.Effects;
using Nexus.Rules;

namespace Nexus.Blood
{
    public class GhoulBloodMetabolism : BaseBloodMetabolism<EnthralledGhoul>
    {
        const int RATE = Vitae.Metab_Settings.DEFAULT;
        public GhoulBloodMetabolism(EnthralledGhoul ghoul) : base(ghoul)
        {
        }
        public override void Cycle()
        {
            base.SetWater();
            if (NotAtMinimum())
            {
                Source.Blood -= RATE;
                if (StatusChange(out var status))
                    CheckStatus(status);
            }
            else
                Metaboliser.Die(); //just like that
        }

        void CheckStatus(BloodLevel status)
        {
            if (status < BloodLevel.QUENCHED)
            {
                if (!Source.Bloodstarved)
                {
                    Source.DisplayName = "{{r|bloodstarved}}";
                    Source.Bloodstarved = true;
                }
                if (Source.Master.HasLOSTo(Metaboliser))
                    IComponent<GameObject>.AddPlayerMessage($"{Metaboliser.t()} feels " + "{{R|thirsty}}.");
            }
            else if (Source.Bloodstarved)
            {
                Source.DisplayName = "{{r|ghoul}}";
                Source.Bloodstarved = false;
            }
        }

        bool StatusChange(out BloodLevel value)
        {
            string status = StatusToString(out value);
            if (Source.LastStatus == status)
                return false;
            Source.LastStatus = status;
            return true;
        }
    }
}