
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

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class VampiricSpell : IScribedPart
    {
        public const string CATEGORY = "Blood Magic";
        public Guid SpellID = Guid.Empty;
        public abstract int Cooldown { get; }
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
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, ParentObject, CATEGORY, this);
        public void ExpendBlood(bool noPopup, string text) => SpellCore.ExpendBlood(noPopup, text, ParentObject, Cost);
        public void ExpendBlood() => SpellCore.ExpendBlood(ParentObject, Cost);
        public bool Cast(string toDo) => SpellCore.Cast(toDo, ParentObject, this, SpellID, Cooldown, Cost, CATEGORY, Name);
    }
}

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class VampireFX : IScribedEffect
    {
        public const string CATEGORY = VampiricSpell.CATEGORY;
        public Guid SpellID = Guid.Empty;
        public abstract int Cooldown { get; }
        public virtual int Cost => VITAE.BLOOD_PER_SIP; //default 10k 
        public override bool WantEvent(int ID, int Cascade) //current use of FX is very temporary so there is no need for CollecStats or AbilityManager stuff
        {
            if (ID == PooledEvent<CommandEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, Object, CATEGORY, this);
        public void ExpendBlood() => SpellCore.ExpendBlood(Object, Cost);
        public bool Cast(string toDo) => SpellCore.Cast(toDo, Object, this, SpellID, Cooldown, Cost, CATEGORY, ClassName);
    }
}



namespace Nexus.Spells
{
    public static class SpellCore
    {
        public static int Roll(GameObject gameObj, int level) => WikiRng.Next(1, 8) + Math.Max(gameObj.StatMod("Ego"), level) + gameObj.GetStat("Level").Value;
        public static bool EnoughBlood(string text, GameObject parentObj, int cost)
        {
            if (parentObj.GetIntProperty(FLAGS.BLOOD_VALUE) > cost)
                return true;
            else
                return parentObj.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }
        public static bool SunlightInterference(GameObject parentObject)
        {
            if (Options.GetOptionBool(OPTIONS.NIGHTBEAST))
            {
                if (Calendar.IsDay() && (parentObject.CurrentZone?.IsOutside() ?? false))
                    return true;
            }
            return false;
        }
        public static bool Cast<T>(string toDo, GameObject parentObject, T invoker, Guid spellID, int cooldown, int cost, string category, string typeName) where T : IComponent<GameObject>
        {
            if (SunlightInterference(parentObject))
            {
                Popup.Show("You are powerless before the gross incandescence of the Sun!");
            }
            else if (EnoughBlood(toDo, parentObject, cost))
            {
                IComponent<GameObject>.AddPlayerMessage("You invoke {{R|blood magic}}.");
                parentObject.SmallTeleportSwirl(null, "&R");
                parentObject.UseEnergy(1000, $"{category} {typeName}");
                invoker.CooldownMyActivatedAbility(spellID, cooldown);
                return true;
            }
            return false;
        }
        public static bool RealityCheck<T>(Cell cell, GameObject parentObject, string category, T invoker) where T : IComponent<GameObject>
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", parentObject, $"{category}", invoker, "Cell", cell);
            if (!parentObject.FireEvent(E) || !parentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(parentObject);
                return false;
            }
            return true;
        }

        public static void ExpendBlood(bool noPopup, string text, GameObject parentObj, int cost)
        {
            if (noPopup)
                IComponent<GameObject>.AddPlayerMessage(text);
            else
                Popup.Show(text);
            ExpendBlood(parentObj, cost);
        }
        //ExpendBlood should be invoked after Cast() returns true
        public static void ExpendBlood(GameObject ParentObject, int Cost)
        {
            ParentObject.GetPart<Vitae>().SubtractBlood(Cost);
        }
    }
}


