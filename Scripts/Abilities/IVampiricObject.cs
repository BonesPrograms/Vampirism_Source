
using System;
using Nexus.Spells;
using Nexus.Rules;
using XRL.World.Effects;
using Nexus.Core;
using XRL.World;
using System.Collections.Generic;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

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

        public Guid _ID = Guid.Empty;
        public virtual Guid ID
        {
            get=>_ID;
            set
            {
                _ID = value;
            }
        }
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
            RemoveMyActivatedAbility(ref _ID);
            ParentObject.RemovePart(this);
        }
        public abstract void CollectStats(Templates.StatCollector stats);
        public virtual int Roll() => IVampiricSpell.Roll(ParentObject, Level);
        Vitae _Vitae;
        public Vitae Vitae => _Vitae ?? ParentObject.GetPart<Vitae>();
        Vampirism _Base;
        public Vampirism Base => _Base ?? ParentObject.GetPart<Vampirism>();

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
        public bool NotEnoughBlood(string text)
        {
            bool count = Vitae.Blood <= VITAE.BLOOD_PER_SIP;
            if (count)
                UI.Popup.Show("You don't have enough {{R|blood}} " + text + "!");
            return count;
        }
        public virtual bool Prerequisites(GameObject Target)
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", ParentObject, "Vampiric Power", this, "Cell", Target.CurrentCell);
            if (!ParentObject.FireEvent(E) || !ParentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                return false;
            }
            return true;
        }
        public virtual void SyncLevels(int NewLevel)
        {
            if (ShouldSync())
                Level = NewLevel;
        }
        public virtual void ExpendBlood(bool iskey, string text, int cost)
        {
            if (iskey)
                AddPlayerMessage(text);
            else
                UI.Popup.Show(text);
            Vitae.SubtractBlood(cost);
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

