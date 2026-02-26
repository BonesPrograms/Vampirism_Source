
using System;
using Nexus.Rules;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Properties;
using Nexus.Spells;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using XRL.UI;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class VampiricSpell : IScribedPart
    {
        public const string CLASS = "Vampiric Spell";
        public Guid SpellID = Guid.Empty;
        public abstract int Cooldown
        {
            get;
        }
        public int Level => ParentObject.GetPart<Vampirism>().Level;
        public virtual int Cost => VITAE.BLOOD_PER_SIP; //default 10k  
        public abstract void AddSpell();
        public abstract void CollectStats(Templates.StatCollector stats);
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
        public virtual int Roll() => SpellCore.Roll(ParentObject, Level);
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, ParentObject, CLASS, this);
        public void ExpendBlood(bool DontPopup, string text) => SpellCore.ExpendBlood(DontPopup, text, ParentObject, Cost);
        public void ExpendBlood() => SpellCore.ExpendBlood(ParentObject, Cost);
        public bool Cast(string ToDo) => SpellCore.Cast(ToDo, ParentObject, this, SpellID, Cooldown, Cost, CLASS, Name);
    }
}

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class VampireFX : IScribedEffect
    {
        public const string CLASS = VampiricSpell.CLASS;
        public virtual int Cost => VITAE.BLOOD_PER_SIP; //default 10k 
        public virtual int Cooldown => 0;
        public abstract Type SpellType
        {
            get;
        }
        public override bool WantEvent(int ID, int Cascade) //current use of FX is very temporary so there is no need for CollecStats or AbilityManager stuff
        {
            if (ID == CommandEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, Object, CLASS, this);
        public void ExpendBlood() => SpellCore.ExpendBlood(Object, Cost);
        public bool Cast(string ToDo) => SpellCore.Cast(ToDo, Object, this, ID, Cooldown, Cost, CLASS, ClassName);
    }
}



namespace Nexus.Spells
{
    public static class SpellCore
    {
        public static int Roll(GameObject Object, int Level) => WikiRng.Next(1, 8) + Math.Max(Object.StatMod("Ego"), Level) + Object.GetStat("Level").Value;
        public static bool EnoughBlood(string text, GameObject ParentObject, int Cost)
        {
            if (ParentObject.GetIntProperty(FLAGS.BLOOD_VALUE) > Cost)
                return true;
            else
                return ParentObject.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }
        public static bool SunlightInterference(GameObject ParentObject)
        {
            if (Options.GetOptionBool(OPTIONS.NIGHTBEAST))
            {
                if (Calendar.IsDay() && (ParentObject.CurrentZone?.IsOutside() ?? false))
                    return true;
            }
            return false;
        }
        public static bool Cast<T>(string ToDo, GameObject ParentObject, T part, Guid SpellID, int Cooldown, int Cost, string CLASS, string Name) where T : IComponent<GameObject>
        {
            if (SunlightInterference(ParentObject))
            {
                Popup.Show("You are powerless before the gross incandescence of the Sun!");
            }
            else if (EnoughBlood(ToDo, ParentObject, Cost))
            {
                IComponent<GameObject>.AddPlayerMessage("You invoke {{R|blood magic}}.");
                ParentObject.SmallTeleportSwirl(null, "&R");
                ParentObject.UseEnergy(1000, $"{CLASS} {Name}");
                part.CooldownMyActivatedAbility(SpellID, Cooldown);
                return true;
            }
            return false;
        }
        public static bool RealityCheck<T>(Cell cell, GameObject obj, string CLASS, T Class) where T : IComponent<GameObject>
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", obj, $"{CLASS}", Class, "Cell", cell);
            if (!obj.FireEvent(E) || !obj.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(obj);
                return false;
            }
            return true;
        }

        public static void ExpendBlood(bool DontPopup, string text, GameObject ParentObject, int Cost)
        {
            if (DontPopup)
                IComponent<GameObject>.AddPlayerMessage(text);
            else
                Popup.Show(text);
            ExpendBlood(ParentObject, Cost);
        }
        //ExpendBlood should be invoked after Cast() returns true
        public static void ExpendBlood(GameObject ParentObject, int Cost)
        {
            ParentObject.GetPart<Vitae>().SubtractBlood(Cost);
        }
    }
}


