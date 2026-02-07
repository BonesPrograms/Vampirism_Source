using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;
using Nexus.Update;
using XRL.World.Parts;
using Nexus.Core;
using Nexus.Rules;
using XRL.UI;

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
            VampireBuilder.ChangeCorpse(GO);
            Spells(GO);
        }

        static void Spells(GameObject GO)
        {
            if (Options.GetOptionBool(OPTIONS.SPELLS))
                VampireBuilder.RequireVampiricObjects(GO);
            else
                VampireBuilder.RemoveVampiricObjects(GO);
        }
    }
}