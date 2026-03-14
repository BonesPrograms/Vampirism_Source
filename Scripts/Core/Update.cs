using XRL;
using XRL.World;
using VampirismSys.Core;
using VampirismSys.Rules;
using XRL.UI;
using XRL.World.Parts;
using VampirismSys.Properties;
using System.Linq;

namespace VampirismSys.Update
{

    [HasCallAfterGameLoaded]
    internal static class Update
    {

        [CallAfterGameLoaded]
        internal static void MyLoadGameCallback()
        {
            if (The.Player.IsVampire())
            {
                Update.TryUpdatePlayer(The.Player);
                Update.Spells(The.Player);
            }
        }
        static void TryUpdatePlayer(GameObject GO)
        {
            string property = GO.GetStringProperty(Flags.Mod.GAMEOBJECT_VERSION_TAG);
            if (TryUpdateNPC(GO) || property != Mod.VERSION)
            {
                UpdateModVersion(GO, property);
            }
        }


        //MOD_VERSION check was added for updating from vers 2 to 3
        //during that time its only purpose was to show a popup saying True Undead has been released
        //however this is also when the "Old Save" property changed to the "Mod Version" flag which is checked every gameload
        internal static bool TryUpdateNPC(GameObject GO)
        {
            if (!GO.HasPart<VampireAshes>())
            {
                VampireBuilder.ChangeCorpse(GO);
                return true;
            }
            return false;
        }
        static void UpdateModVersion(GameObject GO, string property)
        {
            string lastVersion = property ?? "Pre-Versioning";
            GO.SetStringProperty(Flags.Mod.OLD_SAVE, lastVersion);
            GO.SetStringProperty(Flags.Mod.GAMEOBJECT_VERSION_TAG, Mod.VERSION);
        }                                                     
                                                              

        // static void UpdateProperties(GameObject GO)
        // {
        //     VampireBuilder.StringProperties.Select(x => x.Item1).Where(x => GO.Property[x] == Flags.TRUE_LEGACY).ForEach(x => GO.Property[x] = Flags.TRUE);
        // } 

        internal static void Spells(GameObject GO)
        {
            if (VampireBuilder.ENABLE_SPELLS)
            {
                bool WantsSpells = Options.GetOptionBool(ModOptions.SPELLS);
                if (GO.TryGetStringProperty(Flags.SPELLS, out string prop)) //this is just so we dont run through the builder every time you load in or something
                {
                    if (prop == Flags.TRUE)
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
}