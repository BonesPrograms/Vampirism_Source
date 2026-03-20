using XRL;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Core
{

    [HasGameBasedStaticCache]
    internal static class PlayerFinder
    {

        internal static GameObject Player => _playerCache?.Object; //this is used for two major purposes: accessing the players humanity and checking hostility
                                                                   //if you try to access by the.player (static) then you will get whatever
        [GameBasedStaticCache(false)]                       //gameobject they are currently dominating
        static GameObjectReference _playerCache;
        internal static bool Security() => !Player?.HasHitpoints() ?? true ? AssignPlayer() : Player.HasPart<Vampirism>();
        //because you can die but still not be null and the system will break if you are domination-hopping to a new body
        static bool AssignPlayer()
        {
            _playerCache = FindAndCheck().Reference();
            return Player.HasPart<Vampirism>();
        }
        static GameObject FindAndCheck()
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