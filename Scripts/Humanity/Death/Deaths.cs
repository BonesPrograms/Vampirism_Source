using XRL.World;
using XRL.World.Effects;
using XRL;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;

namespace Nexus.Death
{

    /// <summary>
    /// Collection of types of deaths that can reduce humanity.
    /// </summary>
    class Deaths
    {
        readonly GameObject Player;
        readonly GameObject Dying;
        readonly GameObject Killer;
        readonly bool friendly;
        readonly bool hostile;
        public Deaths(GameObject Player, GameObject Dying, GameObject Killer, bool friendly, bool hostile)
        {
            this.Player = Player;
            this.Dying = Dying;
            this.Killer = Killer;
            this.friendly = friendly;
            this.hostile = hostile;
        }

        void GetAndDunk() => Player.GetPart<Humanity>().VampireKilled();
        public void Possibilities()
        {
            if (Fractus())
                return;
            if (Bleed())
                return;
            if (Bloody())
                return;
            if (Victim())
                return;
        }

        bool Victim()
        {
            bool victim = Dying.HasLongProperty(FLAGS.VICTIM);
            bool hostile = Dying.HasLongProperty(FLAGS.VICTIM_HOSTILE);
            if (Killer == The.Player && (victim || hostile && friendly))
            {
                GetAndDunk();
                string flag = victim ? FLAGS.VICTIM : FLAGS.VICTIM_HOSTILE;
                Victim Victim = new(Dying);
                return friendly ? Victim.Friendlybool(flag) : Victim.Victimbool();
            }
            else
                return false;
        }

        /// <summary>
        /// Dunks you for killing someone on a fractus for blood.
        /// </summary>
        bool Fractus()
        {
            if (Dying.CurrentCell?.HasObjectWithPart(nameof(Fracti)) ?? false)
            {
                IComponent<GameObject>.AddPlayerMessage("That was exceptionally cruel. You lose humanity."); //because killing people with a fractus for blood and then losing humanity is funny
                GetAndDunk();
                return true;
            }
            else
                return false;

        }

        /// <summary>
        /// Knows with high certainty if the player is bloodletting their companion.
        /// </summary>
        bool Bleed() //these checks look miserable but i assure you it is important to be extremely explicit when playing around with death events. especially when you risk getting put into gameover by the mod's logic. this shit needs to be iron solid.
        {
            if (!hostile && friendly && Dying.TryGetEffect<Bleeding>(out var bleed) && (bleed.Owner == The.Player || bleed.Owner == Dying))
            {
                string message = Dying.HasEffect<Dominated>() ? "For your cruel act of forcing a victim to bleed themselves to death against their own will, you lose humanity." : "For bleeding your companion to death, you lose humanity.";
                IComponent<GameObject>.AddPlayerMessage(message);
                GetAndDunk();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Attempts to gauge if the player is trying to bloodlet a companion and failing to do it properly.
        /// </summary>

        bool Bloody() //it is important to be as extremely specific as possible like i said. annoyingly long chain but it avoids misfires + considers all possible variations + has extra redundancy
        {
            if (!hostile && friendly && ((Killer == Dying && Dying.HasEffect<Dominated>()) || (Killer == The.Player && The.Player.Target == Dying && The.Player.DistanceTo(Dying) <= 1 && The.Player.IsEngagedInMelee())))
                if (Dying.TryGetEffect<LiquidCovered>(out LiquidCovered e))
                    return CheckLiquid(e);
            return false;
        }

        bool CheckLiquid(LiquidCovered e)
        {
            if (e.Liquid.ContainsLiquid("blood"))
            {
                string message = Dying.HasEffect<Dominated>() ? "For your cruel act of forcing a victim to bleed themselves to death against their own will, you lose humanity." : "For bleeding your companion to death, you lose humanity.";
                IComponent<GameObject>.AddPlayerMessage(message);
                GetAndDunk();
                return true;
            }
            else
                return false;

        }
    }
}