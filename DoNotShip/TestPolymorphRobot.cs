using XRL.World.Parts;
using XRL;
using XRL.Wish;
using XRL.World.Effects;
using XRL.World;
using System;

namespace VampirismSys.Tests
{
    [HasWishCommand]

    public static class RobotPolymorphWishCommand
    {
        [WishCommand("rpt")]

        public static void RobotWishCommandTest()
        {
            The.Player.AddPart<RobotPolymorph>().AddSpell();
        }
    }

[Serializable]
    public class RobotPolymorph : BasePolymorphSpell
    {
        public override BasePolymorphFX PolymorphFX => new RobotPolymorphFX();
        public override int Cooldown => 0;
        public RobotPolymorph()
        {
            CommandName = "RobotPolymorph";
            AbilityMenuName = "Robot Polymorph";
            HUDName = "transform";
            FormName = "Robotform";
        }
    }

}

[Serializable]
public class RobotPolymorphFX : BasePolymorphFX
{
    public RobotPolymorphFX() : base()
    {
        Blueprint = GameObjectFactory.Factory.GetBlueprint("Chromeling");
    }
}
