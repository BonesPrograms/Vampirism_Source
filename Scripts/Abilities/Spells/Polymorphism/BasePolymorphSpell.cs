using XRL.World.Effects;
using System;
using VampirismSys.Rules;
using VampirismSys.Core;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class BasePolymorphSpell : BaseVampireSpell
    {
        protected override bool Toggled => true;

        public bool Transformed { get => _transformed; private set { _transformed = value; } }
        protected string FormName { get => _formName; init { _formName = value; } }
        protected string HUDName { get => _hudName; init { _hudName = value; } }
        string _formName;
        string _hudName;
        bool _transformed;
        protected abstract BasePolymorphEffect Effect { get; }
        protected override void CollectStats(Templates.StatCollector stats)
        {
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Cooldown);
        }

        public override void AddSpell()
        {
            if (FormName.IsNullOrEmpty())
                throw new Exception($"Field FormName in BasePolymorphSpell not assigned in {GetType().Name}!");
            if (HUDName.IsNullOrEmpty())
                throw new Exception($"Field HudName in BasePolymorphSpell is not assigned in {GetType().Name}!");
            base.AddSpell();
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Batform.COMMAND_NAME && Checks.Prerequisites(ParentObject, AbilityMenuName, HUDName))
            {
                if (!ParentObject.IsRealityDistortionUsable())
                    RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                else
                    Cast();
            }
            return base.HandleEvent(E);
        }
        void Cast()
        {
            if (base.Cast(HUDName))
            {
                ExpendBlood();
                if (RealityCheck(ParentObject.CurrentCell)) //you can get trapped in batform
                {
                    if (!Transformed)
                    {
                        Transformed = true;
                        ToggleMyActivatedAbility(SpellID, ParentObject, true, Transformed);
                        ParentObject.ApplyEffect(Effect);
                    }
                    else
                    {
                        Transformed = false;
                        ToggleMyActivatedAbility(SpellID, ParentObject, true, Transformed);
                        ParentObject.RemoveEffectDescendedFrom<BasePolymorphEffect>();
                    }
                }
            }
        }

    }
}
