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

        public Victim(GameObject Dying)
        {
            this.Dying = Dying;
        }
        const int _VICTIM_TIMER = 5000;
        const int RECENT = 500;
        public Consequence FriendlyConsequence(string flag)
        {
            GiveTurns(flag, out long turns, out long moment);
            if (MaxTime(turns, moment))
                return Consequence.SKIP;
            else if (Recent(turns, moment))
                IComponent<GameObject>.AddPlayerMessage("For feeding on a companion and then murdering them, you lose humanity.");
            else
                IComponent<GameObject>.AddPlayerMessage("For murdering your companion and former victim, your lose humanity");
            return Consequence.APPLY;
        }

        public Consequence VictimConsequence()
        {
            GiveTurns(Flags.VICTIM, out long turns, out long moment);
            if (MaxTime(turns, moment))
                return Consequence.SKIP;
            else if (Recent(turns, moment))
                IComponent<GameObject>.AddPlayerMessage("For feeding on the innocent and then murdering them, you lose humanity.");
            else
                IComponent<GameObject>.AddPlayerMessage("For murdering one of your former victims, you lose humanity");
            return Consequence.APPLY;
        }

        bool Recent(long turns, long moment) => turns - moment < RECENT;
        bool MaxTime(long turns, long moment) => turns - _VICTIM_TIMER > moment;
        void GiveTurns(string flag, out long turns, out long moment)
        {
            moment = Dying.GetLongProperty(flag);
            turns = The.Game.Turns;
        }
    }
}