using VampirismSys.Properties;
using XRL.World;
using XRL.World.Parts;
using System.Linq;
using VampirismSys.Extensions;
using XRL;

namespace VampirismSys.Stealth
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

        public static bool StealthStage1
        { get => _stealthStage1; private set { _stealthStage1 = value; } }

        public static bool StealthStage2
        { get => _stealthStage2; private set { _stealthStage2 = value; } }

        public static void Halt()
        {
            StealthStage1 = false;
            StealthStage2 = false;
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
                StealthStage1 = true;
                StealthStage2 = false;
                Player.SetStringProperty(Flags.STEALTH, Flags.TRUE);
            }
        }

        static void None(int count)
        {
            if (!_stealthStage2)
            {

                IComponent<GameObject>.AddPlayerMessage(Display(count));
                StealthStage2 = true;
                StealthStage1 = false;
                Player.SetStringProperty(Flags.STEALTH, Flags.TRUE);
            }
        }

        static void Broken(int count)
        {
            if (_stealthStage1 || _stealthStage2)
            {
                IComponent<GameObject>.AddPlayerMessage(Display(count));
                StealthStage2 = false;
                StealthStage1 = false;
                Player.SetStringProperty(Flags.STEALTH, Flags.FALSE);
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