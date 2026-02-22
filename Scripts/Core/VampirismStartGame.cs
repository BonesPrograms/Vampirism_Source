using XRL;
using XRL.World;
using XRL.UI;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts;
using System.Collections.Generic;

[PlayerMutator]
public class VampirismStartGame : IPlayerMutator
{

    static void RemoveTorch(GameObject GO)
    {

        if (Options.GetOptionBool(OPTIONS.FIRE) && Options.GetOptionBool(OPTIONS.TORCH))// && GO?.Equipped?.Blueprint == "Torch")
        {
            var objects = GO.GetEquippedObjects();
            if (objects != null)
                FindTorch(objects);
        }
    }

    static void FindTorch(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].Blueprint == "Torch")
            {
                objects[i].ForceUnequip(true);
                return;
            }
        }
    }

    static void CallStealthReactivation()
    {
        Nightbeast.NeedsReactivate = true; //this feature solely exists so that stealth runs on gamestart
    }           //the player's zone is not active yet when the mutator is running so stealth doesnt function properly. we wait until the beforetakeactionevent is sent to process

    static void GiveCanteen(GameObject GO)
    {
        if (!Options.GetOptionBool(OPTIONS.HUNTER) && Options.GetOptionBool(OPTIONS.BLOODPACK))
            GO.Inventory.AddObject("BloodCanteen");
    }

    static void SetTime()
    {
        if (Options.GetOptionBool(OPTIONS.NIGHTBEAST))
            The.Game.TimeTicks += 600;
    }
    public void mutate(GameObject GO)
    {
        if (GO.IsVampire())
        {
            CallStealthReactivation();
            GiveCanteen(GO);
            SetTime();
            RemoveTorch(GO);

        }
    }

}
