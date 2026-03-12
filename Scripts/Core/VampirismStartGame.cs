using XRL;
using XRL.World;
using XRL.UI;
using VampirismSys.Rules;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Linq;
using VampirismSys.Properties;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Core
{
    [PlayerMutator]
    internal class VampirismStartGame : IPlayerMutator
    {

        static void RemoveTorch(GameObject GO)
        {

            if (Options.GetOptionBool(ModOptions.FIRE) && Options.GetOptionBool(ModOptions.TORCH))// && GO?.Equipped?.Blueprint == "Torch")
            {
                var objects = GO.GetEquippedObjects();
                if (objects != null)
                    FindTorch(objects);
            }
        }

        static void FindTorch(List<GameObject> objects)
        {
            objects.FirstOrDefault(x => x.Blueprint == "Torch")?.ForceUnequip(true);
        }

        static void CallStealthReactivation()
        {
            Nightbeast.NeedsReactivate = true; //this feature solely exists so that stealth runs on gamestart
        }           //the player's zone is not active yet when the mutator is running so stealth doesnt function properly. we wait until the beforetakeactionevent is sent to process

        static void GiveCanteen(GameObject GO)
        {
            if (!Options.GetOptionBool(ModOptions.HUNTER) && Options.GetOptionBool(ModOptions.BLOODPACK))
                GO.Inventory.AddObject("BloodCanteen");
        }

        static void SetTime()
        {
            if (Options.GetOptionBool(ModOptions.NIGHTBEAST))
                Vampirism.AdvanceTimeToNight();
        }
        public void mutate(GameObject GO)
        {
            if (GO.IsVampire())
            {
                GO.SetStringProperty(Flags.MOD_VERSION, Mod.VERSION);
                CallStealthReactivation();
                GiveCanteen(GO);
                SetTime();
                RemoveTorch(GO);

            }
        }

    }
}