using XRL.World.Parts;
using XRL.World;
using Nexus.Core;
using Nexus.Properties;
using XRL.UI;
using XRL.World.Effects;
using System.Text;
using System.Collections.Generic;
using Nexus.Rules;

namespace Nexus.Blood
{
    /// <summary>
    /// Handles the inner logic for metabolizing blood every turn.
    /// </summary>
    public class BloodMetabolism
    {
        readonly XRL.World.Parts.Vitae Source;
        public bool Glut => Source.Blood >= Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Quenched => Source.Blood >= Rules.Vitae.BLOOD_QUENCHED && Source.Blood < Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Thirsty => Source.Blood >= Rules.Vitae.BLOOD_THIRSTY && Source.Blood < Rules.Vitae.BLOOD_QUENCHED;
        public bool Parched => Source.Blood >= Rules.Vitae.BLOOD_PARCHED && Source.Blood < Rules.Vitae.BLOOD_THIRSTY;
        public bool Min => Source.Blood < Rules.Vitae.BLOOD_PARCHED;



        /// <summary>
        /// For water resets only.
        /// </summary>

        public BloodMetabolism(XRL.World.Parts.Vitae Source) => this.Source = Source;
        public void Cycle() //the main thirst method for using your blood as time goes on and giving you Bloodthirst
        {
            if (NotAtMinimum())
            {
                Bleeding();
                Overfed();
                SetBloodValue();
                CheckForBloodlust();
            }
            SetStomachValues();
        }

        void SetStomachValues()
        {
            Stomach s = Source.ParentObject.GetPart<Stomach>();
            Overrides.Water(ref s.Water);
            if (Options.GetOptionBool(ModOptions.TRUE_UNDEAD) && s.HungerLevel != 0)   //most True Undead code is in Vampirism, this is the only one outside of it
                s.ClearHunger();
        }

        void SetBloodValue()
        {
            Source.Blood -= Rules.Vitae.BLOOD_METAB;
            Source.ParentObject.SetStringProperty(Flags.BLOOD_STATUS, StatusToString());
            Source.ParentObject.SetIntProperty(Flags.BLOOD_VALUE, Source.Blood);
        }
        bool NotAtMinimum()
        {
            Source.Blood = Source.Blood <= Rules.Vitae.BLOOD_MIN ? Rules.Vitae.BLOOD_MIN : Source.Blood;
            return Source.Blood > Rules.Vitae.BLOOD_MIN;
        }

        void Overfed()
        {
            if (Source.Blood >= Rules.Vitae.BLOOD_PUKE && !Source.ParentObject.CheckFlag(Flags.FRENZY))
            {
                Popup.Show("You overfed!");
                Overrides.Vomit(Source.ParentObject);
            }
        }

        void Bleeding()
        {
            if (Source.ParentObject.HasEffect<Bleeding>() && Options.GetOptionBool(ModOptions.BLEED_THIRST))
            {
                Source.Blood -= Source.ParentObject.CheckFlag(Flags.FEED) ? Rules.Vitae.BLOOD_PER_BLOODLOSS_FEED : Rules.Vitae.BLOOD_PER_BLOODLOSS;
                IComponent<GameObject>.AddPlayerMessage("Bloodloss makes you {{R|thistier}}!");
            }
        }

        void CheckForBloodlust()
        {
            if (!Source.Bloodlusted && Source.Blood < Rules.Vitae.BLOOD_QUENCHED)
            {
                Source.Bloodlusted = true;
                Source.ParentObject.ApplyEffect(new Bloodlust(9999, Source.GameOver));
            }
        }

        /// <summary>
        /// For use in BLOOD_STATUS in Bloodthirst.
        /// </summary>
        /// <returns></returns>

        string StatusToString()
        {
            if (Glut)
                return nameof(Glut);
            if (Quenched)
                return nameof(Quenched);
            if (Thirsty)
                return nameof(Thirsty);
            if (Parched)
                return nameof(Parched);
            if (Min)
                return nameof(Min);
            return OutOfRange();
        }

        static string OutOfRange()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ BloodMetabolism.TurnBoolToString() -- all values returned false, should not be possible. Will break bloodthirst.");
            return "Error";
        }





    }
}