using XRL.World.AI.Pathfinding;
using VampirismSys.Extensions;
using VampirismSys.Properties;
using XRL.World.Capabilities;
using System;
using System.Linq;
using XRL.World.Effects;
using XRL.World.AI;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Parts;


namespace VampirismSys.Stealth
{
    public class SpotterCore
    {
        readonly GameObject Source;
        readonly Dictionary<GameObject, int> SpotterRanges = new();
        readonly List<GameObject> PotentialSpotters;
        (GameObject Object, int Distance) Spotter = (Object: null, Distance: 0);
        public SpotterCore(GameObject Source, List<GameObject> PotentialSpotters)
        {
            this.Source = Source;
            this.PotentialSpotters = PotentialSpotters;
        }

        public SpotterCore(GameObject Source)
        {
            this.Source = Source;
            this.PotentialSpotters = SpotterCore.GiveDefaultList(Source);
        }
        ///AI_RADIUS+1 to prevent a bug: if AI is 1 tile outside radius and Spotter effect is applied, 
        /// they will move and appear to instantly break stealth the same moment you make an attack
        /// despite UI display saying that stealth is valid.
        /// technically, your stealth state was valid, but some attacks pass the turn the moment they are completed, which gives the aforementioned
        /// APPEARANCE of stealth being broken instantly, as the ai travels one tile into your detection radius.
        /// so the ai isnt really in your detection radius as the enum says, they were actually outside of it, theyre moreso in your "extended" radius
        bool Spotted(int distance, GameObject Spotter) => distance == VampirismSys.Rules.Stealth.AI_RADIUS + 1 && Spotter.HasLOSTo(Source, false);
        static string DefaultMessage(GameObject Spotter) => $"You try to sneak attack, but {Spotter.t()} spots you from a distance!";
        public static List<GameObject> GiveDefaultList(GameObject Source)
        {
            return Source.CurrentZone.CombatObjects(x => StealthCore.ValidSentient(x) && !x.Unaware(false)).ToList();
        }

        /// <summary>
        /// If you plan to use an Alert in response to SPOTTER_IN_DETECTION, you usually will want to use this method, so that you can pass the spotter
        /// as the exposer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Spotter"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Check<T>(out GameObject Spotter, string message = default) where T : IOpinionSubject, new()
        {
            Spotter = ReturnSpotter();
            return Spotter != null && SpotterFound<T>(Spotter, message);
        }
        GameObject ReturnSpotter()
        {
            PotentialSpotters.Where(x => x.canPathTo(Source.CurrentCell)).ForEach(x => SpotterRanges[x] = x.DistanceTo(Source));
            return SpotterRanges.Count == 0 ? null : GetSpotter();
        }
        GameObject GetSpotter()
        {
            int minimumvalue = SpotterRanges.Values.Min();
            SpotterRanges.First(x => x.Value == minimumvalue).Deconstruct(out GameObject key, out int distance);
            Spotter = (key, distance);
            return Spotter.Object;
        }
        bool SpotterFound<T>(GameObject Spotter, string message) where T : IOpinionSubject, new()
        {
            Spotter.ApplyEffect(new Spotter(Source, VampirismSys.Rules.Feed.DURATION));
            if (Spotted(this.Spotter.Distance, this.Spotter.Object))
            {
                message = message == default ? DefaultMessage(Spotter) : message;
                XRL.UI.Popup.Show(message);
                Spotter.AddOpinion<T>(Source);
                return true;
            }
            return false;
        }

    }
}



namespace XRL.World.Effects
{
    /// <summary>
    /// Very simple pathing effect that removes itself when the player's feed is over.
    /// </summary>

    [Serializable]
    public class Spotter : IBeastScribedEffect
    {
        GameObjectReference Player;
        public Spotter()
        {
        }
        public Spotter(GameObject player, int Duration) : this()
        {
            this.Player = player.Reference();
            base.Duration = Duration;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            Duration--;
            if (Duration > 0)
            {
                FindPath findPath = new FindPath(currentCell, Player.Object.CurrentCell, PathGlobal: false, PathUnlimited: true, base.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: false, FlexPhase: false);
                if (!findPath.Usable)
                    Duration = 0;
                else
                    AutoAct.TryToMove(base.Object, currentCell, findPath.Steps[1], findPath.Directions[0], AllowDigging: true, OpenDoors: true, Peaceful: false);
            }
            return base.HandleEvent(E);
        }
        public override bool UseStandardDurationCountdown() => false;

    }
}