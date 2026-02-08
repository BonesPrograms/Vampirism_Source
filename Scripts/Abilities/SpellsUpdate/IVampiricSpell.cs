
using System;
using Nexus.Spells;
using Nexus.Rules;
using XRL.World.Effects;
using Nexus.Core;
using XRL.World;
using Nexus.Properties;

namespace Nexus.Spells
{
    interface IVampiricSpell
    {
        public bool ShouldSync();
        public static int Roll(GameObject Object, int Level) => WikiRng.Next(1, 8) + Math.Max(Object.StatMod("Ego"), Level) + Object.GetStat("Level").Value;
        public int Roll();
        public int Level
        {
            get;
            set;
        }
        public void SyncLevels(int NewLevel);
    }
}


namespace XRL.World.Parts
{

    [Serializable]
    public abstract class VampiricSpell : IScribedPart, IVampiricSpell
    {
        public int _Cost = VITAE.BLOOD_PER_SIP; //default 10k
        public virtual int Cost
        {
            get => _Cost;
            set
            {
                _Cost = value;
            }
        }
        public const string TAG = "Vampiric Spell";
        public Guid ID = Guid.Empty;
        public int _Level = 1; //level will always be synced with vampirism level
        public int Level
        {
            get => _Level;
            set
            {
                _Level = value;
            }
        }
        public abstract bool ShouldSync();
        public abstract void RequireObject();
        public virtual void RemoveObject()
        {
            RemoveMyActivatedAbility(ref ID);
            ParentObject.RemovePart(this);
        }
        public abstract void CollectStats(Templates.StatCollector stats);
        public virtual int Roll() => IVampiricSpell.Roll(ParentObject, Level);
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == PooledEvent<CommandEvent>.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(ID, CollectStats);
            return base.HandleEvent(E);
        }

        public virtual void SyncLevels(int NewLevel)
        {
            if (ShouldSync())
                Level = NewLevel;
        }

        public bool RealityCheck(Cell cell) //get real
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", ParentObject, TAG, this, "Cell", cell);
            if (!ParentObject.FireEvent(E) || !ParentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                return false;
            }
            return true;
        }

        public bool EnoughBlood(string text)
        {
            if (ParentObject.GetIntProperty(FLAGS.BLOOD_VALUE) > Cost)
                return true;
            else
                return ParentObject.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }

        public bool Cast(string text, int cooldown, string msg)
        {
            if (EnoughBlood(msg))
            {
                ParentObject.UseEnergy(1000, $"{TAG} {text}");
                CooldownMyActivatedAbility(ID, cooldown);
                return true;
            }
            return false;
        }
        public void ExpendBlood(bool nopoup, string text)
        {
            if (nopoup)
                AddPlayerMessage(text);
            else
                UI.Popup.Show(text);
            ExpendBlood();
        }
        //ExpendBlood should be invoked after Cast() returns true
        public void ExpendBlood()
        {
            ParentObject.GetPart<Vitae>().SubtractBlood(Cost);
        }
    }
}

namespace XRL.World.Effects
{
    [Serializable]
    public abstract class SpellEffect : IScribedEffect, IVampiricSpell
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

