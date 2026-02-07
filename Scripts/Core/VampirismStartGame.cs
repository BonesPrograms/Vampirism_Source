using XRL;
using XRL.World;
using XRL.UI;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts;

[PlayerMutator]
public class VampirismStartGame : IPlayerMutator
{
    public void mutate(GameObject GO)
    {
        DeathHandler.Player = null; //see more about why we do this in Core
        if (GO.IsVampire())
        {  
            if (!Options.GetOptionBool(OPTIONS.HUNTER) && Options.GetOptionBool(OPTIONS.BLOODPACK))
                GO.Inventory.AddObject("BloodCanteen");
            if (Options.GetOptionBool(OPTIONS.NIGHTBEAST))
                The.Game.TimeTicks += 600;
            if (Options.GetOptionBool(OPTIONS.FIRE) && Options.GetOptionBool(OPTIONS.TORCH))// && GO?.Equipped?.Blueprint == "Torch")
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
