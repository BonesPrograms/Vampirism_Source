using XRL;
using XRL.World;
using Nexus.Update;
using Nexus.Core;
using Nexus.Rules;
using XRL.UI;
using XRL.World.Parts;
using Nexus.Properties;
using System.Linq;

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
            UpdateOldSave(GO); //update rolls once
            Spells(GO); //this is for the option that turns spells on/off on load
        }

        public static void UpdateOldSave(GameObject GO)
        {
            if (DoUpdate(GO))
                MarkAsOldSave(GO);
        }

        public static bool DoUpdate(GameObject GO)
        {
            if (CheckCorpse(GO))
            {
                UpdateProperties(GO);
                return true;
            }
            return false;
        }
        public static void MarkAsOldSave(GameObject GO)
        {
            GO.SetStringProperty(FLAGS.OLD_SAVE, MOD.VERSION); //this may serve as a mod version identifier in the future
        }                                                       //anyone who doesnt have it will get it, anyone who has it and doesnt sync with the version will be updated
                                                                //furthermore, our WantEvent that checks for OLD_SAVE will compare it against the version, rather than check for it in general
        static bool CheckCorpse(GameObject GO)
        {
            if (!GO.HasPart<VampireAshes>())
            {
                VampireBuilder.ChangeCorpse(GO);
                return true;
            }
            return false;
        }

        static void UpdateProperties(GameObject GO)
        {
            VampireBuilder.StringProperties.Select(x => x.Item1).Where(x => GO.Property[x] == FLAGS.TRUE_LEGACY).ForEach(x => GO.Property[x] = FLAGS.TRUE);
        }
        public static void Spells(GameObject GO)
        {
            bool WantsSpells = Options.GetOptionBool(OPTIONS.SPELLS);
            if (GO.TryGetStringProperty(FLAGS.SPELLS, out string prop)) //this is just so we dont run through the builder every time you load in or something
            {
                if (prop == FLAGS.TRUE)
                {
                    if (!WantsSpells)
                        VampireBuilder.RemoveSpells(GO);
                }
                else if (WantsSpells)
                    VampireBuilder.RequireSpells(GO);
            }
            else if (WantsSpells)
                VampireBuilder.RequireSpells(GO);
        }
    }
}