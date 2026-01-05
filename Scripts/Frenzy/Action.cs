using XRL.World;
using XRL.World.Parts;
using XRL.World.Capabilities;
using Nexus.Biting;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Parts.Mutation;
using Nexus.Attack;
using XRL.World.Effects;
using XRL.World.AI.Pathfinding;
using System;

namespace Nexus.Frenzy
{
    /// <summary>
    /// Controls the turn-to-turn decision making for Frenzy.
    /// </summary>
    public class Action
    {
        readonly FrenzyAI AI;
        public Action(FrenzyAI AI) => this.AI = AI;
        static bool BadBite(Bite Bite) => Bite.BadTarget() && Bite.CannotFeed();
        public void Act()
        {
            if (Scan.Incap(AI.Object, true))
                AI.Duration = 0;
            else if (!Scan.ReturnProperty(AI.Object, Flags.FEED))
            {
                if (AI.Object.canPathTo(AI.Target?.CurrentCell)) //canpathto does nullcheck for us
                    DecideAction();
                else
                    FindNewTarget();
            }
        }
        void FindNewTarget()
        {
            if (new Search(AI.Source).TryScan(out GameObject newTarget))
                AI.Target = newTarget;
            else
                AI.Duration = 0;
        }

        void DecideAction()
        {
            if (!AI.InRange)
                Path(new FindPath(AI.Object.CurrentCell, AI.Target.CurrentCell, PathGlobal: false, PathUnlimited: true, AI.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: true, FlexPhase: false));
            else if (!BadBite(new Bite(AI.Object, AI.Target)))
                new VampireAttack(AI.Target, AI.Object.GetPart<Vampirism>()).Attack(true);
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
            if (AI.Source.TargetRegistry.ContainsKey(AI.Target))
                AI.Source.TargetRegistry[AI.Target] = TheBeast.FLAG_AVOID;
            else
                AI.Source.TargetRegistry.Add(AI.Target, TheBeast.FLAG_AVOID);
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