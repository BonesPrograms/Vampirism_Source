using XRL.World;
using Nexus.Core;
using Nexus.Properties;
using XRL.UI;
using XRL.World.Effects;
using Nexus.Rules;

namespace Nexus.Blood
{

    public class VampireBloodMetabolism : BaseBloodMetabolism<XRL.World.Parts.Vitae>
    {
        public VampireBloodMetabolism(XRL.World.Parts.Vitae Source) : base(Source)
        {
        }
        public override void Cycle()
        {
            if (NotAtMinimum())
            {
                Bleeding();
                Overfed();
                SetBloodValue();
                CheckForBloodlust();
            }
            SetStomach();
        }

        void SetStomach()
        {
            SetWater();
            if (Options.GetOptionBool(ModOptions.TRUE_UNDEAD) && Stomach.HungerLevel != 0)   //most True Undead code is in Vampirism, this is the only one outside of it
                Stomach.ClearHunger();
        }

        void SetBloodValue()
        {
            Blood -= Rules.Vitae.BLOOD_METAB;
            Metaboliser.SetStringProperty(Flags.BLOOD_STATUS, StatusToString(out _));
            Metaboliser.SetIntProperty(Flags.BLOOD_VALUE, Blood);
        }

        void Overfed()
        {
            if (Blood >= Rules.Vitae.BLOOD_PUKE && !Metaboliser.CheckFlag(Flags.FRENZY))
            {
                Popup.Show("You overfed!");
                Vomit();
            }
        }

        void Bleeding()
        {
            if (Metaboliser.HasEffect<Bleeding>() && Options.GetOptionBool(ModOptions.BLEED_THIRST))
            {
                Blood -= Metaboliser.CheckFlag(Flags.FEED) ? Rules.Vitae.BLOOD_PERBloodLOSS_FEED : Rules.Vitae.BLOOD_PERBloodLOSS;
                IComponent<GameObject>.AddPlayerMessage("Bloodloss makes you {{R|thistier}}!");
            }
        }

        void CheckForBloodlust()
        {
            if (!Source.Bloodlusted && Blood < Rules.Vitae.BLOOD_QUENCHED)
            {
                Source.Bloodlusted = true;
                Metaboliser.ApplyEffect(new Bloodlust(9999, Source.GameOver));
            }
        }

    }
}