using XRL;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Core
{

    [HasGameBasedStaticCache]
    public static class PlayerFinder
    {
        public static GameObject Player
        {
            get
            {
                if (!_playerCache?.Object?.HasHitpoints() ?? true)
                    _playerCache = Find().Reference();
                return _playerCache.Object;
            }
        }

        [GameBasedStaticCache(false)]
        static GameObjectReference _playerCache;
        static GameObject Find()
        {
            if (The.Player.TryGetEffect(out Dominated e))
                return LoopDominator(e);
            else if (The.Player.TryGetPart(out Vehicle v))
                return CheckPilot(v.Pilot);
            return The.Player;
        }

        static GameObject CheckPilot(GameObject pilot)
        {
            if (pilot.TryGetEffect(out Dominated e))
                return LoopDominator(e);
            return pilot;
        }

        /// <summary>
        /// Loops through the domination effect's dominator to find the player's actual GameObject and assign it to the Player field.
        /// </summary>
        /// <returns></returns>
        static GameObject LoopDominator(Dominated e)
        {
            GameObject TrueDominator = e.Dominator;
            while (TrueDominator.HasEffect<Dominated>())
            {
                Dominated d = TrueDominator.GetEffect<Dominated>();
                TrueDominator = d.Dominator;
            }
            if (TrueDominator.TryGetPart(out Vehicle v))
                return CheckPilot(v.Pilot);
            return TrueDominator;
        }

    }
}