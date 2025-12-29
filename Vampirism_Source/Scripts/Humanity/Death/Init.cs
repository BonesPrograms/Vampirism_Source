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
            if (!Scan.Applicable(GO)) //cleanup - i had no way of specifying this kind of check in the xml. gets rid of targets that arent actually able to be fed on by vampires
            {
                GO.RemovePart<XRL.World.Parts.DeathHandler>();
                return false;
            }
            Innocent(GO, Player);
            return true;
        }
        static void Innocent(GameObject GO, GameObject Player)
        {
            if (!GO.HasStringProperty(Flags.INNOCENT))
                GO.SetStringProperty(Flags.INNOCENT, GO.IsHostileTowards(Player) ? Flags.FALSE : Flags.TRUE);
        }

    }
}