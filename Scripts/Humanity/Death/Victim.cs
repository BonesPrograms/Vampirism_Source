using XRL.World;
using XRL;
using Nexus.Properties;

namespace Nexus.Death
{
    /// <summary>
    /// Static helper for victim death cases to keep things clean.
    /// </summary>
    class Victim
    {


        readonly GameObject Dying;
        public Victim(GameObject Dying) => this.Dying = Dying;
        const int _VICTIM_TIMER = 5000;
        const int RECENT = 100;
        public bool Friendlybool(string flag)
        {
            GiveTurns(flag, out long turns, out long moment);
            if (!MaxTime(turns, moment))
            {
                string message = Recent(turns, moment) ? "For feeding on a companion and then murdering them, you lose humanity." : "For murdering your companion and former victim, your lose humanity";
                IComponent<GameObject>.AddPlayerMessage(message);
                return true;
            }
            return false;
        }

        public bool Victimbool()
        {
            GiveTurns(FLAGS.VICTIM, out long turns, out long moment);
            if (!MaxTime(turns, moment))
            {
                string message = Recent(turns, moment) ? "For feeding on the innocent and then murdering them, you lose humanity." : "For murdering one of your former victims, you lose humanity";
                IComponent<GameObject>.AddPlayerMessage(message);
                return true;
            }
            return false;
        }

        static bool Recent(long turns, long moment) => turns - moment < RECENT;
        static bool MaxTime(long turns, long moment) => turns - _VICTIM_TIMER > moment;
        void GiveTurns(string flag, out long turns, out long moment)
        {
            moment = Dying.GetLongProperty(flag);
            turns = The.Game.Turns;
        }
    }
}