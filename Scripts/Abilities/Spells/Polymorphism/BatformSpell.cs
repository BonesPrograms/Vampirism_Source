using XRL.World.Effects;
using System;
using VampirismSys.Rules;



namespace XRL.World.Parts
{

    [Serializable]
    public class BatformSpell : BasePolymorphSpell
    {
        public override int Cooldown => Batform.COOLDOWN;
        public override BasePolymorphFX PolymorphFX => new BatformFX();
        public BatformSpell()
        {
            CommandName = Batform.COMMAND_NAME;
            AbilityMenuName = Batform.ABILITY_NAME;
            HUDName = "transform";
            FormName = "Batform";
        }
    }
}