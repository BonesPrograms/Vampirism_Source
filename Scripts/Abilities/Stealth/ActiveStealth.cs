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
    [HasGameBasedStaticCache]
    public static class ActiveStealth //i plan to one day, maybe, turn this into an "Actual" UI, so i made it a separate class to avoid serialization issues down the line post-release
    {
        static GameObject Player => The.Player;
        const int SINGLE = 1;
        const int NONE = 0;
        static int ActiveWitnessCount => StealthCore.TrueCount;

        /// <summary>
        /// Stage one means that there is only one witness.
        /// </summary>
        /// 
        [GameBasedStaticCache]
        static bool _stealthStage1 = default;

        /// <summary>
        /// Stage two means there are no witnesses.
        /// </summary>
        /// 
        [GameBasedStaticCache]
        static bool _stealthStage2 = default;

        public static bool StealthStage1 => _stealthStage1;

        public static bool StealthStage2 => _stealthStage2;

        public static void Halt()
        {
            _stealthStage1 = false;
            _stealthStage2 = false;
        }
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
            if (!_stealthStage1)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                _stealthStage1 = true;
                _stealthStage2 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void None(int count)
        {
            if (!_stealthStage2)
            {

                IComponent<GameObject>.AddPlayerMessage(Display(count));
                _stealthStage2 = true;
                _stealthStage1 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void Broken(int count)
        {
            if (_stealthStage1 || _stealthStage2)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                _stealthStage2 = false;
                _stealthStage1 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
            }
        }
        static string Display(int count)
         =>
            count switch
            {
                NONE => "{{B|No witnesses.}}",
                SINGLE => "{{O|" + Nightbeast.Witnesses.First(x => x.Value).Key.t() + " is the only witness.}}",
                _ => "{{R|Witnesses!}}",
            };


    }
}