
using System;
using Nexus.Rules;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Parts.Mutation;


namespace XRL.World.Parts
{

    [Serializable]
    public abstract class VampiricSpell : IScribedPart
    {
        [NonSerialized]
        public const string CLASS = "Vampiric Spell";
        public Guid SpellID = Guid.Empty;
        public int Level => ParentObject.GetPart<Vampirism>().Level; //mutation level is separate from baselevle so i didnt want to create all this complicated shit to track it
        public abstract int Cooldown();                              //because if player character's  level is too low, then their mutation levels are limited
        public abstract string SpellType();                           //instead of some dyanmic system that listens for levelups and does all this calculation, we just map to the current given level
        public abstract void AddSpell();
        public abstract void CollectStats(Templates.StatCollector stats);
        public virtual int Cost() => VITAE.BLOOD_PER_SIP; //default 10k
        public static int Roll(GameObject Object, int Level) => WikiRng.Next(1, 8) + Math.Max(Object.StatMod("Ego"), Level) + Object.GetStat("Level").Value;
        public virtual int Roll() => Roll(ParentObject, Level);
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == PooledEvent<CommandEvent>.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(SpellID, CollectStats);
            return base.HandleEvent(E);
        }

        public virtual void RemoveSpell()
        {
            RemoveMyActivatedAbility(ref SpellID);
            ParentObject.RemovePart(this);
        }

        public virtual void SyncLevels(int BaseLevel, int ActualLevel)
        {
                
        }

        public bool RealityCheck(Cell cell) //get real
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", ParentObject, CLASS, this, "Cell", cell);
            if (!ParentObject.FireEvent(E) || !ParentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                return false;
            }
            return true;
        }

        public bool EnoughBlood(string text)
        {
            if (ParentObject.GetIntProperty(FLAGS.BLOOD_VALUE) > Cost())
                return true;
            else
                return ParentObject.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }

        public bool Cast(string ToDo)
        {
            if (EnoughBlood(ToDo))
            {
                ParentObject.UseEnergy(1000, $"{CLASS} {SpellType()}");
                CooldownMyActivatedAbility(SpellID, Cooldown());
                return true;
            }
            return false;
        }
        public void ExpendBlood(bool DontPopup, string text)
        {
            if (DontPopup)
                AddPlayerMessage(text);
            else
                UI.Popup.Show(text);
            ExpendBlood();
        }
        //ExpendBlood should be invoked after Cast() returns true
        public void ExpendBlood()
        {
            ParentObject.GetPart<Vitae>().SubtractBlood(Cost());
        }
    }
}

namespace XRL.World.Effects
{
    [Serializable]
    public abstract class SpellEffect : IScribedEffect
    {
        public abstract bool ShouldSync();
        public abstract int Roll();
        public virtual void SyncLevels(int NewLevel)
        {
            if (ShouldSync())
                Level = NewLevel;
        }
        public int _Level = 1;
        public int Level
        {
            get => _Level;
            set
            {
                _Level = value;
            }
        }
    }
}

