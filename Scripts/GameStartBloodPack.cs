using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.UI;
using Nexus.Rules;

[PlayerMutator]
public class GameStartBloodPack : IPlayerMutator
{
    public void mutate(GameObject GO)
    {
       if (!Options.GetOptionBool(OPTIONS.HUNTER) && Options.GetOptionBool(OPTIONS.BLOODPACK) && GO.HasPart<Vampirism>())
            GO.Inventory.AddObject("BloodCanteen");
    }
}