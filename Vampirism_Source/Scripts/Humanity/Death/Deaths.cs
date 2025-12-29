using XRL.World;
using XRL.World.Effects;
using XRL;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;

namespace Nexus.Death
{


    enum Consequence
    {
        SKIP,
        APPLY
    }

    /// <summary>
    /// Collection of types of deaths that can reduce humanity.
    /// </summary>
    class Deaths
    {
        readonly GameObject Player;
        readonly GameObject Dying;
        readonly GameObject Killer;
        public Deaths(GameObject Player, GameObject Dying, GameObject Killer)
        {
            this.Player = Player;
            this.Dying = Dying;
            this.Killer = Killer;
        }

        void GetAndDunk() => Player.GetPart<Humanity>().VampireKilled();
        public void Run()
        {
            if (Fractus() == Consequence.APPLY)
                return;
            if (Bleed() == Consequence.APPLY)
                return;
            if (Bloody() == Consequence.APPLY)
                return;
            if (Victim() == Consequence.APPLY)
                return;
        }

        Consequence Victim()
        {
            if (Killer == The.Player && (Dying.HasLongProperty(Flags.VICTIM) || (Dying.HasLongProperty(Flags.VICTIM_HOSTILE) && Scan.IsFriendly(Dying, The.Player))))
            {
                GetData(out bool victim, out bool friendly, out Victim Victim);
                GetAndDunk();
                return CheckVictimType(Victim, victim, friendly);
            }
            else
                return Consequence.SKIP;
        }

        void GetData(out bool victim, out bool friendly, out Victim Victim)
        {
            victim = Dying.HasLongProperty(Flags.VICTIM);
            friendly = Scan.IsFriendly(Dying, The.Player);
            Victim = new Victim(Dying);
        }

        Consequence CheckVictimType(Victim Victim, bool victim, bool friendly)
        {
            if (friendly)
            {
                if (victim)
                    return Victim.FriendlyConsequence(Flags.VICTIM);
                if (Dying.HasLongProperty(Flags.VICTIM_HOSTILE))
                    return Victim.FriendlyConsequence(Flags.VICTIM_HOSTILE);
            }
            return Victim.VictimConsequence();
        }

        /// <summary>
        /// Dunks you for killing someone on a fractus for blood.
        /// </summary>

        Consequence Fractus()
        {
            if (Dying.CurrentCell?.HasObjectWithPart(nameof(Fracti)) is true)
            {
                IComponent<GameObject>.AddPlayerMessage("That was exceptionally cruel. You lose humanity."); //because killing people with a fractus for blood and then losing humanity is funny
                GetAndDunk();
                return Consequence.APPLY;
            }
            else
                return Consequence.SKIP;

        }

        /// <summary>
        /// Knows with high certainty if the player is bloodletting their companion.
        /// </summary>
        Consequence Bleed() //these checks look miserable but i assure you it is important to be extremely explicit when playing around with death events. especially when you risk getting put into gameover by the mod's logic. this shit needs to be iron solid.
        {
            if (!Dying.IsHostileTowards(The.Player) && Dying.HasEffect<Bleeding>() && Scan.IsFriendly(Dying, The.Player) && (Dying.GetEffect<Bleeding>().Owner == The.Player || Dying.GetEffect<Bleeding>().Owner == Dying))
            {
                if (!Dying.HasEffect<Dominated>())
                    IComponent<GameObject>.AddPlayerMessage("For bleeding your companion to death, you lose humanity.");
                else
                    IComponent<GameObject>.AddPlayerMessage("For your cruel act of forcing a victim to bleed themselves to death against their own will, you lose humanity.");
                GetAndDunk();
                return Consequence.APPLY;
            }
            else
                return Consequence.SKIP;
        }

        /// <summary>
        /// Attempts to gauge if the player is trying to bloodlet a companion and failing to do it properly.
        /// </summary>

        Consequence Bloody() //it is important to be as extremely specific as possible like i said. annoyingly long chain but it avoids misfires + considers all possible variations + has extra redundancy
        {
            if (!Dying.IsHostileTowards(The.Player) && Scan.IsFriendly(Dying, The.Player) && ((Killer == Dying && Dying.Target is null && Dying.HasEffect<Dominated>()) || (Killer == The.Player && The.Player.Target == Dying && The.Player.DistanceTo(Dying) <= 1 && The.Player.IsEngagedInMelee())))
            {
                if (Dying.TryGetEffect<LiquidCovered>(out LiquidCovered e))
                    return CheckLiquid(e);
            }
            return Consequence.SKIP;
        }

        Consequence CheckLiquid(LiquidCovered e)
        {
            if (e.Liquid.ContainsLiquid("blood"))
            {
                if (!Dying.HasEffect<Dominated>())
                    IComponent<GameObject>.AddPlayerMessage("For bleeding your companion to death, you lose humanity.");
                else
                    IComponent<GameObject>.AddPlayerMessage("For your cruel act of forcing a victim to bleed themselves to death against their own will, you lose humanity.");
                GetAndDunk();
                return Consequence.APPLY;
            }
            else
                return Consequence.SKIP;

        }
    }
}