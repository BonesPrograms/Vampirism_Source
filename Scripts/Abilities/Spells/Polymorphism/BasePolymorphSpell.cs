using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class BasePolymorphSpell : BaseVampireSpell //the original version used metamorphosis to turn you into a literal bat, but your party would not sync and i didnt feel like trying to fix that
    {                                           //because the alternative is easier: fake transformation as you see in this type. there are also tons of other issues like mutations and stats and precognition not easily being synced so this is optimal
        public bool Transformed => ParentObject.IsPolymorphed();     
        public abstract string FormName { get; }
        public abstract string HUDName { get; }
        public abstract BasePolymorphFX PolymorphFX { get; }
        public override void AddSpell()
        {
            SpellID = AddMyActivatedAbility(AbilityMenuName, CommandName, CATEGORY, null, "\u009f", Toggleable: true);
        }
        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Cooldown);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Batform.COMMAND_NAME && Checks.Prerequisites(ParentObject, AbilityMenuName, HUDName))
            {
                if (!ParentObject.IsRealityDistortionUsable())
                    RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                else if (!Transformed)
                    Cast();
                else
                {
                    ToggleMyActivatedAbility(SpellID, ParentObject, true);
                    ParentObject.RemoveEffectDescendedFrom<BasePolymorphFX>();
                }
            }
            return base.HandleEvent(E);
        }
        void Cast()
        {
            if (Cast(HUDName))
            {
                ExpendBlood();
                if (RealityCheck(ParentObject.CurrentCell))
                {
                    ToggleMyActivatedAbility(SpellID, ParentObject, true);
                    ParentObject.ApplyEffect(PolymorphFX);
                }
            }
        }

    }
}
