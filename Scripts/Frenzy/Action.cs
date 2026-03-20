using XRL.World;
using XRL.World.Parts;
using XRL.World.Capabilities;
using VampirismSys.Biting;
using VampirismSys.Extensions;
using VampirismSys.Properties;
using VampirismSys.Attack;
using XRL.World.Effects;
using XRL.World.AI.Pathfinding;

using XRL.World.Parts.Mutation;

namespace VampirismSys.Frenzy
{
    /// <summary>
    /// Controls the turn-to-turn decision making for Frenzy.
    /// </summary>
    public class ActionAI
    {
        readonly FrenzyAI AI;
        readonly Search Search;
        readonly Bite Bite;
        public ActionAI(FrenzyAI AI)
        {
            this.AI = AI;
            Bite = AI.Source.Base.FeedAbility.Bite;
            Search = AI.Source.Core.Search;
        }
        bool BadBite(GameObject Target) => Bite.BadTarget(Target) && Bite.CannotFeed(Target);

		bool AICantFrenzy() //feed and frenzy checks removed compared to TheBeast.CantFrenzy()
		{
			return AI.Source.Base.Rotschrek || !AI.Source.HasFangs() || AI.Source.Incap() || Vampirism.SunlightInterference(AI.Object);
		}
        public void Act()
        {
            if (AICantFrenzy())
                AI.Duration = 0;
            else if (!AI.Object.CheckFlag(Flags.FEED))
            {
                if (AI.Target?.HasHitpoints() ?? false && AI.Object.canPathTo(AI.Target.CurrentCell) && Checks.IsNotASolidBlock(AI.Target)) //canpathto does nullcheck for us
                    DecideAction();
                else
                    FindNewTarget();
            }
        }
        void FindNewTarget()
        {
            if (Search.TryScan(out GameObject newTarget))
            {
                AI.Target = newTarget;
            }
            else
                AI.Duration = 0;
        }

        void DecideAction()
        {
            if (!AI.InRange)
                Path(new FindPath(AI.Object.CurrentCell, AI.Target.CurrentCell, PathGlobal: false, PathUnlimited: true, AI.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: true, FlexPhase: false));
            else if (BadBite(AI.Target))
                BiteFailed();
            else
                new VampireAttack(AI.Target, AI.Source.Base).Attack(true);

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