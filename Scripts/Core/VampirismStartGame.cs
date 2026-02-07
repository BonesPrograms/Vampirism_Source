using XRL;
using XRL.World;
using XRL.UI;
using Nexus.Rules;
using Nexus.Core;
using Nexus.Update;

[PlayerMutator]
public class VampirismStartGame : IPlayerMutator
{
    public void mutate(GameObject GO)
    {
        if (GO.IsVampire())
        {
            if (!Options.GetOptionBool(OPTIONS.HUNTER) && Options.GetOptionBool(OPTIONS.BLOODPACK))
                GO.Inventory.AddObject("BloodCanteen");
            if (Options.GetOptionBool(OPTIONS.NIGHTBEAST))
                The.Game.TimeTicks += 600;
            if (Options.GetOptionBool(OPTIONS.FIRE))// && GO?.Equipped?.Blueprint == "Torch")
            {
                // GO.Equipped.Obliterate();
                var objects = GO.GetEquippedObjects();
                if (objects != null)
                    for (int i = 0; i < objects.Count; i++)
                        if (objects[i].Blueprint == "Torch")
                        {
                            objects[i].Obliterate();
                            return;
                        }
            }
        }
    }

}
