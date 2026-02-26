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
		static bool _StealthStage1 = default;

		/// <summary>
		/// Stage two means there are no witnesses.
		/// </summary>
		/// 
		[GameBasedStaticCache]
		static bool _StealthStage2 = default;

        public static bool StealthStage1 => _StealthStage1;

        public static bool StealthStage2 => _StealthStage2;

        public static void Halt()
        {
            _StealthStage1 = false;
            _StealthStage2 = false;
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
            if (!_StealthStage1)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                _StealthStage1 = true;
                _StealthStage2 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void None(int count)
        {
            if (!_StealthStage2)
            {

                    IComponent<GameObject>.AddPlayerMessage(Display(count));
                _StealthStage2 = true;
                _StealthStage1 = false;
               Player.SetStringProperty(FLAGS.STEALTH, FLAGS.TRUE);
            }
        }

        static void Broken(int count)
        {
            if (_StealthStage1 || _StealthStage2)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                _StealthStage2 = false;
                _StealthStage1 = false;
                Player.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
            }
        }
        static string Display(int count)
         =>
            count switch
            {
                NONE => "{{B|No witnesses.}}",
                SINGLE => "{{O|" + Nightbeast.Witnesses.PickFirstEqualTo(true).Key.t() + " is the only witness.}}",
                _ => "{{R|Witnesses!}}",
            };


    }
}