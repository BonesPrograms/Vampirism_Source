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
        readonly Vitae Source;
        public bool Glut => Source.Blood >= VITAE.BLOOD_GLUTTONOUS;
        public bool Quenched => Source.Blood >= VITAE.BLOOD_QUENCHED && Source.Blood < VITAE.BLOOD_GLUTTONOUS;
        public bool Thirsty => Source.Blood >= VITAE.BLOOD_THIRSTY && Source.Blood < VITAE.BLOOD_QUENCHED;
        public bool Parched => Source.Blood >= VITAE.BLOOD_PARCHED && Source.Blood < VITAE.BLOOD_THIRSTY;
        public bool Min => Source.Blood < VITAE.BLOOD_PARCHED;

     

        /// <summary>
        /// For water resets only.
        /// </summary>

        public BloodMetabolism(Vitae Source) => this.Source = Source;
        public void Cycle() //the main thirst method for using your blood as time goes on and giving you Bloodthirst
        {
            if (NotAtMinimum())
            {
                Bleeding();
                Overfed();
                SetBloodValue();
                CheckForBloodlust();
            }
            Overrides.Water(ref Source.ParentObject.GetPart<Stomach>().Water);
        }

        void SetBloodValue()
        {
            Source.Blood -= VITAE.BLOOD_METAB;
            Source.ParentObject.SetStringProperty(FLAGS.BLOOD_STATUS, TurnBoolToString());
            Source.ParentObject.SetIntProperty(FLAGS.BLOOD_VALUE, Source.Blood);
        }
        bool NotAtMinimum()
        {
            Source.Blood = Source.Blood <= VITAE.BLOOD_MIN ? VITAE.BLOOD_MIN : Source.Blood;
            return Source.Blood > VITAE.BLOOD_MIN;
        }

        void Overfed()
        {
            if (Source.Blood >= VITAE.BLOOD_PUKE && !Source.ParentObject.CheckFlag(FLAGS.FRENZY))
            {
                Popup.Show("You overfed!");
                Overrides.Vomit(Source.ParentObject);
            }
        }

        void Bleeding()
        {
            if (Source.ParentObject.HasEffect<Bleeding>())
            {
                Source.Blood -= Source.ParentObject.CheckFlag(FLAGS.FEED) ? VITAE.BLOOD_PER_BLOODLOSS_FEED : VITAE.BLOOD_PER_BLOODLOSS;
                IComponent<GameObject>.AddPlayerMessage("You feel {{R|thistier}}!");
            }
        }

        void CheckForBloodlust()
        {
            if (!Source.Bloodlusted && Source.Blood < VITAE.BLOOD_QUENCHED)
            {
                Source.SetBloodlust(true);
                Source.ParentObject.ApplyEffect(new Bloodlust(9999, Source.GameOver));
            }
        }

        /// <summary>
        /// For use in BLOOD_STATUS in Bloodthirst.
        /// </summary>
        /// <returns></returns>

        string TurnBoolToString()
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
            else
                return OutOfRange();
        }

        static string OutOfRange()
        {
             MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ BloodMetabolism.TurnBoolToString() -- all values returned false, should not be possible. Will break bloodthirst.");
             return "Error";
        }





    }
}