using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;
using Nexus.Update;
using XRL.World.Parts;
using Nexus.Core;
using Nexus.Rules;
using XRL.UI;
using Nexus.Properties;

[HasCallAfterGameLoaded]
public static class VampirismUpdater
{
    [CallAfterGameLoaded]
    public static void MyLoadGameCallback()
    {
        if (The.Player.IsVampire())
            Update.Check(The.Player);
    }
}
namespace Nexus.Update
{
    static class Update
    {
        public static void Check(GameObject GO)
        {
            CheckCorpse(GO);
            Spells(GO);
        }

        static void CheckCorpse(GameObject GO)
        {
            if (GO.TryGetStringProperty(FLAGS.CORPSE, out string result))
            {
                if (result != FLAGS.TRUE)
                    VampireBuilder.ChangeCorpse(GO);
            }
            else
                VampireBuilder.ChangeCorpse(GO);
        }

        static void Spells(GameObject GO)
        {
            bool WantsSpells = Options.GetOptionBool(OPTIONS.SPELLS);
            if (GO.TryGetStringProperty(FLAGS.SPELLS, out string prop))
            {
                if (prop == FLAGS.TRUE)
                {
                    if (!WantsSpells)
                        VampireBuilder.RemoveVampiricObjects(GO);
                }
                else if (WantsSpells)
                    VampireBuilder.RequireVampiricObjects(GO);
            }
            else if (WantsSpells)
                VampireBuilder.RequireVampiricObjects(GO);
        }
    }
}