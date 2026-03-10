
using System;
using Nexus.Rules;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Properties;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using XRL.UI;
using XRL.World.AI;
using System.Linq;
using UnityEngine.UI;

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class BaseVampireSpell : IScribedPart
    {
        public Guid SpellID = Guid.Empty;
        public const string CATEGORY = "Blood Magic";
        public string CommandName; //should be assigned in public parameterless constructor
        public string AbilityMenuName;
        public int Level => ParentObject.GetPart<Vampirism>().Level;
        public virtual int Cost => Nexus.Rules.Vitae.BLOOD_PER_SIP; //default 10k  
        public abstract int Cooldown { get; } //these are getter-only so that they can be easily changed in the future if i want
        public virtual bool Toggled => false; //this is only for AddMyActivatedAbility. you need to track the actual toggled on/off state yourself
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
        public virtual int Roll()
        {
            return WikiRng.Next(1, 8) + Math.Max(ParentObject.StatMod("Ego"), Level) + ParentObject.GetStat("Level").Value;
        }
        public virtual void AddSpell()
        {
            if (CommandName.IsNullOrEmpty())
                throw new Exception($"CommandName not assigned to {GetType().Name}!");
            if (AbilityMenuName.IsNullOrEmpty())
                throw new Exception($"AbiltiyMenuName not assigned to {GetType().Name}!");
            SpellID = AddMyActivatedAbility(AbilityMenuName, CommandName, CATEGORY, null, "\u009f", Toggleable: Toggled);
        }
        public virtual void RemoveSpell()
        {
            RemoveMyActivatedAbility(ref SpellID);
            ParentObject.RemovePart(this);
        }
        public bool EnoughBlood(string text, int cost)
        {
            if (ParentObject.GetIntProperty(Flags.BLOOD_VALUE) > cost)
                return true;
            else
                return ParentObject.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }
        public bool Cast(string toDo)
        {
            if (Vampirism.SunlightInterference(ParentObject))
            {
                Popup.Show("You are powerless before the gross incandescence of the Sun!");
            }
            else if (EnoughBlood(toDo, Cost))
            {
                IComponent<GameObject>.AddPlayerMessage("You invoke {{R|blood magic}}.");
                ParentObject.SmallTeleportSwirl(null, "&R");
                ParentObject.UseEnergy(1000, $"{CATEGORY} {AbilityMenuName}");
                CooldownMyActivatedAbility(SpellID, Cooldown);
                return true;
            }
            return false;
        }
        public bool RealityCheck(Cell cell) => RealityCheck(cell, CATEGORY, this); //i already made this and im lazy and dont feel like rewriting all my reality checks for the static method

        public void ExpendBlood(bool noPopup, string text)
        {
            if (noPopup)
                IComponent<GameObject>.AddPlayerMessage(text);
            else
                Popup.Show(text);
            ExpendBlood();
        }
        //ExpendBlood should be invoked after Cast() returns true
        public void ExpendBlood()
        {
            ParentObject.GetPart<XRL.World.Parts.Vitae>().Blood -= Cost;
        }

        public static bool RealityCheck<T>(Cell cell, string category, T invoker) where T : IPart
        {
            GameObject parentObject = invoker.ParentObject;
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", parentObject, $"{category}", invoker, "Cell", cell);
            if (!parentObject.FireEvent(E) || !parentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(parentObject);
                return false;
            }
            return true;
        }
    }
}