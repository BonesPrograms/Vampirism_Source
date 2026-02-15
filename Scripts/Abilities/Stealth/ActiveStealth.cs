using Nexus.Properties;
using XRL.World;
using XRL.World.Parts;
using System.Linq;
using Nexus.Core;
using XRL;

namespace Nexus.Stealth
{

    /// <summary>
    /// Handles UI and sets global stealth flags.
    /// </summary>
    public static class ActiveStealth //i plan to one day, maybe, turn this into an "Actual" UI, so i made it a separate class to avoid serialization issues down the line post-release
    {
        public static GameObject Player => The.Player;
        const int SINGLE = 1;
        const int NONE = 0;
        static int ActiveWitnessCount => Nightbeast.TrueCount;
        public static void SetStealth()
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

        static void Single(int count)
        {
            if (!Nightbeast.StealthStage1)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                Nightbeast.StealthStage1 = true;
                Nightbeast.StealthStage2 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void None(int count)
        {
            if (!Nightbeast.StealthStage2)
            {

                    IComponent<GameObject>.AddPlayerMessage(Display(count));
                Nightbeast.StealthStage2 = true;
                Nightbeast.StealthStage1 = false;
               Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void Broken(int count)
        {
            if (Nightbeast.StealthStage1 || Nightbeast.StealthStage2)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                Nightbeast.StealthStage2 = false;
                Nightbeast.StealthStage1 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
            }
        }
        static string Display(int count)
         =>
            count switch
            {
                NONE => "{{B|No witnesses.}}",
                SINGLE => "{{O|" + Nightbeast.Witnesses.PickFirst(true).Key.t() + " is the only witness.}}",
                _ => "{{R|Witnesses!}}",
            };


    }
}