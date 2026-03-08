using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;



namespace XRL.World.Parts
{

    [Serializable]
    public class BatformSpell : BasePolymorphSpell
    {
        public override string CommandName => Batform.COMMAND_NAME;
        public override string AbilityMenuName => Batform.ABILITY_NAME;
        public override int Cooldown => Batform.COOLDOWN;
        public override string HUDName => "transform";
        public override string FormName => "Batform";
        public override BasePolymorphFX PolymorphFX => new BatformFX();
    }
}