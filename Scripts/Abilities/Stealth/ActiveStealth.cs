using Nexus.Properties;
using XRL.World;
using XRL.World.Parts;
using System.Linq;

namespace Nexus.Stealth
{

    /// <summary>
    /// Handles UI and sets global stealth flags.
    /// </summary>
    public class ActiveStealth //i plan to one day, maybe, turn this into an "Actual" UI, so i made it a separate class to avoid serialization issues down the line post-release
    {
        readonly Nightbeast Source;
        const int SINGLE = 1;
        const int NONE = 0;
        public ActiveStealth(Nightbeast Source)
        {
            this.Source = Source;
        }
        public void SetStealth(int ActiveWitnessCount)
        {
            switch (ActiveWitnessCount)
            {
                case SINGLE:
                    Single(ActiveWitnessCount);
                    break;
                case NONE:
                    None(ActiveWitnessCount);
                    break;
                default:
                    Broken(ActiveWitnessCount);
                    break;
            }
        }

        void Single(int count)
        {
            if (!Source.StealthStage1)
            {

                    IComponent<GameObject>.AddPlayerMessage(Display(count));
                Source.StealthStage1 = true;
                Source.StealthStage2 = false;
                Source.ParentObject.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        void None(int count)
        {
            if (!Source.StealthStage2)
            {

                    IComponent<GameObject>.AddPlayerMessage(Display(count));
                Source.StealthStage2 = true;
                Source.StealthStage1 = false;
                Source.ParentObject.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        void Broken(int count)
        {
            if (Source.StealthStage1 || Source.StealthStage2)
            {

                    IComponent<GameObject>.AddPlayerMessage(Display(count));
                Source.StealthStage2 = false;
                Source.StealthStage1 = false;
                Source.ParentObject.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
            }
        }
        string Display(int count)
         =>
            count switch
            {
                NONE => "{{B|No witnesses.}}",
                SINGLE => "{{O|" + Source.ActiveWitnesses[0].t() + " is the only witness.}}",
                _ => "{{R|Witnesses!}}",
            };


    }
}