using XRL.World;
using Nexus.Properties;
using Nexus.Core;

namespace Nexus.Death
{
    /// <summary>
    /// Static evaluator for removing parts from invalid targets and applying Innocence values.
    /// </summary>
    static class Init
    {
        public static bool Evaluate(GameObject GO, GameObject Player)
        {
            if (!GO.IsPlayer())
            {
                if (!Checks.Applicable(GO)) //cleanup - i had no way of specifying this kind of check in the xml. gets rid of targets that arent actually able to be fed on by vampires
                {
                    GO.RemovePart<XRL.World.Parts.DeathHandler>(); //furthermore, i use death handler for some update-related stuff, so we do not remove it from the player
                    return false;                                   //even if the player is non-applicable (playing as a creature that normally shouldnt be able to become a vampire via embrace)
                }
                Innocent(GO, Player);
            }
            return true;
        }
        static void Innocent(GameObject GO, GameObject Player)
        {
            if (!GO.HasStringProperty(FLAGS.INNOCENT))
                GO.SetStringProperty(FLAGS.INNOCENT, GO.IsHostileTowards(Player) ? FLAGS.FALSE : FLAGS.TRUE);
        }

    }
}