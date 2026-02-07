using XRL.World;
using XRL.World.Parts;
using XRL.World.Capabilities;
using Nexus.Biting;
using Nexus.Core;
using Nexus.Properties;
using Nexus.Attack;
using XRL.World.Effects;
using XRL.World.AI.Pathfinding;

namespace Nexus.Frenzy
{
    /// <summary>
    /// Controls the turn-to-turn decision making for Frenzy.
    /// </summary>
    public class Action
    {
        readonly FrenzyAI AI;
        readonly Search Search;
        readonly Bite Bite;
        public Action(FrenzyAI AI, Bite Bite, Search Search)
        {
            this.AI = AI;
            this.Bite = Bite;
            this.Search = Search;
        }
        bool BadBite(GameObject Target) => Bite.BadTarget(Target) && Bite.CannotFeed(Target);
        public void Act()
        {
            if (AI.Object.Incap(true))
                AI.Duration = 0;
            else if (!AI.Object.CheckFlag(FLAGS.FEED))
            {
                if (AI.Object.canPathTo(AI.Target?.CurrentCell)) //canpathto does nullcheck for us
                    DecideAction();
                else
                    FindNewTarget();
            }
        }
        void FindNewTarget()
        {
            if (Search.TryScan(out GameObject newTarget))
                AI.Target = newTarget;
            else
                AI.Duration = 0;
        }

        void DecideAction()
        {
            if (!AI.InRange)
                Path(new FindPath(AI.Object.CurrentCell, AI.Target.CurrentCell, PathGlobal: false, PathUnlimited: true, AI.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: true, FlexPhase: false));
            else if (!BadBite(AI.Target))
            {
                new VampireAttack(AI.Target, AI.Source.Base, AI.Source.Base.GetDamageDice(), AI.Target.IsFriendly(AI.Object)).Attack(true);
            }
            else
                BiteFailed();
        }
        void BiteFailed()
        {
            IComponent<GameObject>.AddPlayerMessage("{{R|The Beast}} doesn't like this one.");
            if (AI.gameover)
            {
                if (AI.Target?.HasHitpoints() ?? false)
                    RegisterBadTarget();
                AI.Target = null;
            }
            else
                AI.Duration = 0;
        }
        void RegisterBadTarget()
        {
            AI.Source.TargetRegistry[AI.Target] = TheBeast.FLAG_AVOID;
        }
        void Path(FindPath findPath)
        {
            if (!findPath.Usable)
            {
                //  if (AI.Target.HasHitpoints())
                //   IComponent<GameObject>.AddPlayerMessage("You can't find a way to reach " + AI.Target.t() + ".");
                AI.Target = null; //used to set duration to 0 but not anymore >:)
            }
            else
                AutoAct.TryToMove(AI.Object, AI.Object.CurrentCell, findPath.Steps[1], findPath.Directions[0], AllowDigging: true, OpenDoors: true, Peaceful: false);
        }
    }
}