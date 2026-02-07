using XRL.World.AI.Pathfinding;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Capabilities;
using System;
using System.Linq;
using XRL.World.Effects;
using XRL.World.AI;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Parts;


namespace Nexus.Stealth
{
    enum Spot
    {
        /// <summary>
        /// If this value is returned, then no one on the list was able to path to the player.
        /// </summary>
        SPOTTER_IS_NULL,
        /// <summary>
        /// Remedy for a bug that is explained in Spotted(int, GameObject). If this value is returned, should divert to a non-stealth attack.
        /// </summary>
        SPOTTER_IN_DETECTION,
        /// <summary>
        /// This value implies that the Spotter effect has been applied to someone and stealth can proceed.
        /// </summary>
        SPOTTER_OUTSIDE_DETECTION
    }

    class SpotterGenerator
    {
        readonly Nightbeast Source;
        readonly Dictionary<GameObject, int> SpotterRanges = new();
        KeyValuePair<GameObject, int> package;
        readonly List<GameObject> PotentialSpotters;
        public SpotterGenerator(Nightbeast Source, List<GameObject> PotentialSpotters)
        {
            this.Source = Source;
            this.PotentialSpotters = PotentialSpotters;
        }

        ///AI_RADIUS+1 to prevent a bug: if AI is 1 tile outside radius and Spotter effect is applied, 
        /// they will move and appear to instantly break stealth the same moment you make an attack
        /// despite UI display saying that stealth is valid.
        /// technically, your stealth state was valid, but some attacks pass the turn the moment they are completed, which gives the aforementioned
        /// APPEARANCE of stealth being broken instantly, as the ai travels one tile into your detection radius.
        /// 
        bool Spotted(int distance, GameObject Spotter) => distance == Nexus.Rules.STEALTH.AI_RADIUS + 1 && Spotter.HasLOSTo(Source.ParentObject);
        static string DefaultMessage(GameObject Spotter) => $"You try to sneak attack, but {Spotter.t()} spots you from a distance!";
        public static List<GameObject> GiveDefaultList(Nightbeast Source)
        {
            List<GameObject> local = new();
            for (int i = 0; i < Source.ValidSentients.Count; i++)
            {
                GameObject witness = Source.ValidSentients[i];
                if (!witness.Unaware(false) && !StealthCore.Inanimate(witness))
                    local.Add(witness);
            }
            return local;
        }
        public Spot BeginAttackCheckIfSpotted<T>(string message = default) where T : IOpinionSubject, new()
        {
            GameObject Spotter = ReturnSpotter();
            return Spotter is null ? Spot.SPOTTER_IS_NULL : SpotterFound<T>(Spotter, message);
        }
        /// <summary>
        /// If you plan to use an Alert in response to SPOTTER_IN_DETECTION, you usually will want to use this method, so that you can pass the spotter
        /// as the exposer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Spotter"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Spot BeginAttackCheckIfSpotted<T>(out GameObject Spotter, string message = default) where T : IOpinionSubject, new()
        {
            Spotter = ReturnSpotter();
            return Spotter is null ? Spot.SPOTTER_IS_NULL : SpotterFound<T>(Spotter, message);
        }
        GameObject ReturnSpotter()
        {
            for (int i = 0; i < PotentialSpotters.Count; i++)
            {
                GameObject witness = PotentialSpotters[i];
                if (witness.canPathTo(Source.ParentObject.CurrentCell))
                    SpotterRanges.Add(witness, witness.DistanceTo(Source.ParentObject));
            }
            return SpotterRanges.Count == 0 ? null : SpotterRanges.Count == 1 ? OneKey() : ManyKeys();
        }

        GameObject OneKey()
        {
            package = SpotterRanges.ElementAt(0);
            return package.Key;
        }
        GameObject ManyKeys()
        {
            int minimumvalue = SpotterRanges.Values.Min();
            package = SpotterRanges.First(x => x.Value == minimumvalue);
            return package.Key;
        }
        Spot SpotterFound<T>(GameObject Spotter, string message) where T : IOpinionSubject, new()
        {
            Spot spot = Spotted(package.Value, package.Key) ? Spot.SPOTTER_IN_DETECTION : Spot.SPOTTER_OUTSIDE_DETECTION;
            if (spot == Spot.SPOTTER_IN_DETECTION)
            {
                message = message == default ? DefaultMessage(Spotter) : message;
                XRL.UI.Popup.Show(message);
                Spotter.AddOpinion<T>(Source.ParentObject);
                Spotter.ApplyEffect(new Spotter(Source.ParentObject, Nexus.Rules.FEED.DURATION, true));
            }
            else
                Spotter.ApplyEffect(new Spotter(Source.ParentObject, Nexus.Rules.FEED.DURATION, false));
            return spot;
        }

    }
}

namespace XRL.World.Effects
{
    /// <summary>
    /// Very simple pathing effect that removes itself when the player's feed is over.
    /// </summary>

    [Serializable]
    public class Spotter : Effect
    {
        public GameObjectReference Player;
        public Spotter() => DisplayName = "";
        bool pathonly;
        public Spotter(GameObject player, int Duration, bool pathonly) : this()
        {
            this.Player = player.Reference();
            base.Duration = Duration;
            this.pathonly = pathonly;

        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if ((pathonly && Duration > 0) || (Duration = !Player.Object.CheckFlag(FLAGS.FEED) ? default : Duration) != 0)
            {
                FindPath findPath = new FindPath(currentCell, Player.Object.CurrentCell, PathGlobal: false, PathUnlimited: true, base.Object, 500, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: false, FlexPhase: false);
                if (!findPath.Usable)
                    Duration = 0;
                else
                    AutoAct.TryToMove(base.Object, currentCell, findPath.Steps[1], findPath.Directions[0], AllowDigging: true, OpenDoors: true, Peaceful: false);
            }
            return base.HandleEvent(E);
        }

        public override bool UseStandardDurationCountdown() => true;
    }
}