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
                Update.TryUpdatePlayerOnly(The.Player);
                Update.Spells(The.Player);
            }
        }
        internal static void TryUpdatePlayerOnly(GameObject GO)
        {
            if (TryUpdateNPCFriendly(GO) || GO.GetStringProperty(Flags.MOD_VERSION) != Mod.VERSION)
            {
                Popup.Show("Vampirism mini update: True Undead released! See steam page for more info.");
                UpdateModVersion(GO);
            }
        }


        //MOD_VERSION check was added for updating from vers 2 to 3
        //during that time its only purpose was to show a popup saying True Undead has been released
        //however this is also when the "Old Save" property changed to the "Mod Version" flag which is checked every gameload
        internal static bool TryUpdateNPCFriendly(GameObject GO)
        {
            if (CheckCorpse(GO))
            {
                UpdateProperties(GO); //checkcorpse and UpdateProperties are for updating from  vers 1 to 2
                return true;
            }
            return false;
        }
        internal static void UpdateModVersion(GameObject GO)
        {
            GO.SetStringProperty(Flags.MOD_VERSION, Mod.VERSION); //this may serve as a mod version identifier in the future
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
            VampireBuilder.StringProperties.Select(x => x.Item1).Where(x => GO.Property[x] == Flags.TRUE_LEGACY).ForEach(x => GO.Property[x] = Flags.TRUE);
        }
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