using XRL.World.Effects;
using System;
using VampirismSys.Rules;
using VampirismSys.Core;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class BasePolymorphSpell : BaseVampireSpell //the original version used metamorphosis to turn you into a literal bat, but your party would not sync and i didnt feel like trying to fix that
    {                                           //because the alternative is easier: fake transformation as you see in this type. there are also tons of other issues like mutations and stats and precognition not easily being synced so this is optimal
   
        public override bool Toggled => true;

        public bool Transformed
        {
            get => _transformed;
            private set
            {
                _transformed = value;
            }
        }
        public string FormName
        {
            get=>_formName;
            protected init
            {
                _formName = value;
            }
        }

        public string HUDName
        {
            get=>_hudName;
            protected init
            {
                _hudName = value;
            }
        }

        string _formName;

        string _hudName;

        bool _transformed;
        protected abstract BasePolymorphEffect Effect { get; }
        public override void CollectStats(Templates.StatCollector stats)
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

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.Write(_formName);
            Writer.Write(_hudName);
            Writer.Write(_transformed);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            _formName = Reader.ReadString();
            _hudName = Reader.ReadString();
            _transformed = Reader.ReadBoolean();
            base.Read(Basis, Reader);
        }

    }
}
