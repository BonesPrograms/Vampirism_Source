using XRL.World;
using XRL.World.Parts;
using XRL.World.Capabilities;
using Nexus.Bite;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Parts.Mutation;
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
        public Action(FrenzyAI AI) => this.AI = AI;
        Command Bite() => Biting.TryCreateInstance(AI.Object, AI.Target, out Biting Bite) && Bite.CannotFeed() ? BiteFailed() : Command.CONTINUE;
        
        Command ScanForTargets() => AI.Object.canPathTo(AI.Target?.CurrentCell) ? Command.CONTINUE : ValidateScan();
        
        Command EvaluateState() => Scan.Incap(AI.Object, true) ? Command.END : Command.CONTINUE;
        public void Act()
        {
            if (EvaluateState() == Command.END)
            {
                AI.Duration = 0;
                return;
            }
            if (ScanForTargets() == Command.END)
                return;
            DecideAction();

        }

        Command ValidateScan()
        {
            if (new Search(AI.Source).TryScan(out GameObject newTarget))
            {
                AI.Target = newTarget;
                return Command.CONTINUE;
            }
            else
            {
                AI.Duration = 0;
                return Command.END;
            }
        }

        void DecideAction()
        {
            bool feeding = Scan.SafeReturnProperty(AI.Object, Flags.FEED);
            FindTarget(feeding);
            if (AI.InRange && !feeding && Bite() == Command.CONTINUE)
                new VampireAttack(AI.Target, AI.Object.GetPart<Vampirism>()).Attack();
        }
        Command BiteFailed()
        {
            IComponent<GameObject>.AddPlayerMessage("{{R|The Beast}} doesn't like this one.");
            if (AI.gameover)
            {
                AI.Source.TargetRegistry[AI.Target] = TheBeast.FLAG_AVOID;
                AI.Target = null;
            }
            else
                AI.Duration = 0;
            return Command.END;
        }
        void FindTarget(bool feeding)
        {
            if (!AI.InRange && !feeding)
            {
                FindPath findPath = new(AI.Object.CurrentCell, AI.Target.CurrentCell, PathGlobal: false, PathUnlimited: true, AI.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: true, FlexPhase: false);
                if (!findPath.Usable)
                {
                    if (AI.Target.HasHitpoints())
                        IComponent<GameObject>.AddPlayerMessage("You can't find a way to reach " + AI.Target.t() + ".");
                    AI.Duration = 0;
                }
                else
                    AutoAct.TryToMove(AI.Object, AI.Object.CurrentCell, findPath.Steps[1], findPath.Directions[0], AllowDigging: true, OpenDoors: true, Peaceful: false);
            }
        }
    }

}
