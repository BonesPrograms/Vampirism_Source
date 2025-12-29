using XRL.World.AI.Pathfinding;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Capabilities;
using System;

namespace XRL.World.Effects
{
    /// <summary>
    /// Very simple pathing effect that removes itself when the player's feed is over.
    /// </summary>

    [Serializable]
    public class Spotter : Effect
    {
        public GameObjectReference Player;
        public Spotter()
        {
            DisplayName = "";
        }

        public Spotter(GameObject player, int Duration) : this()
        {
            this.Player = player.Reference();
            base.Duration = Duration;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (!base.WantEvent(ID, Cascade))
                return ID == SingletonEvent<EndTurnEvent>.ID;
            return true;
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            bool feeding = Scan.SafeReturnProperty(Player.Object, Flags.FEED);
            if (!feeding)
                Duration = 0;
            else
            {
                FindPath findPath = new FindPath(currentCell, Player.Object.CurrentCell, PathGlobal: false, PathUnlimited: true, base.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: false, FlexPhase: false);
                if (!findPath.Usable)
                    Duration = 0;
                else
                    AutoAct.TryToMove(base.Object, currentCell, findPath.Steps[1], findPath.Directions[0], AllowDigging: true, OpenDoors: true, Peaceful: false);
            }
            return base.HandleEvent(E);
        }
    }
}