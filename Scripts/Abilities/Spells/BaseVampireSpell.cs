
using System;
using VampirismSys.Rules;
using XRL.World.Effects;
using VampirismSys.Extensions;
using VampirismSys.Properties;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using XRL.UI;
using XRL.World.AI;
using System.Linq;
using UnityEngine.UI;
using VampirismSys.Core;

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class BaseVampireSpell : IBeastScribedPart
    {
        public const string CATEGORY = "Blood Magic";
        public Guid SpellID { get => _spellID; private set { _spellID = value; } }
        public string CommandName { get => _commandName; init { _commandName = value; } }
        public string AbilityMenuName { get => _abilityName; init { _abilityName = value; } }

        Guid _spellID = Guid.Empty;

        string _commandName;

        string _abilityName;

        public int Level => ParentObject.GetPart<Vampirism>().Level;

        protected virtual int Cost => VampirismSys.Rules.Metab.BLOOD_PER_SIP; //default 10k  

        protected abstract int Cooldown { get; } //these are getter-only so that they can be easily changed in the future if i want

        protected virtual bool Toggled => false; //this is only for AddMyActivatedAbility. you need to track the actual toggled on/off state yourself

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

        protected abstract void CollectStats(Templates.StatCollector stats);

        protected virtual int Roll() => WikiRng.Next(1, 8) + Math.Max(ParentObject.StatMod("Ego"), Level) + ParentObject.GetStat("Level").Value;

        public virtual void AddSpell()
        {
            VerifyInitialization();
            SpellID = AddMyActivatedAbility(AbilityMenuName, CommandName, CATEGORY, null, "\u009f", Toggleable: Toggled);
        }
        public virtual void RemoveSpell()
        {
            RemoveMyActivatedAbility(ref _spellID);
            ParentObject.RemovePart(this);
        }

        protected bool Cast(string toDo)
        {
            if (Vampirism.SunlightInterference(ParentObject))
            {
                Popup.Show("You are powerless before the gross incandescence of the Sun!");
            }
            else if (EnoughBlood(toDo))
            {
                IComponent<GameObject>.AddPlayerMessage("You invoke {{R|blood magic}}.");
                ParentObject.SmallTeleportSwirl(null, "&R");
                ParentObject.UseEnergy(1000, $"{CATEGORY} {AbilityMenuName}");
                CooldownMyActivatedAbility(SpellID, Cooldown);
                return true;
            }
            return false;
        }
        protected bool RealityCheck(Cell cell) => RealityCheck(cell, CATEGORY, this); //i already made this and im lazy and dont feel like rewriting all my reality checks for the static method

        protected void ExpendBlood(bool showPopup, string text)
        {
            if (!showPopup)
                IComponent<GameObject>.AddPlayerMessage(text);
            else
                Popup.Show(text);
            ExpendBlood();
        }
        //ExpendBlood should be invoked after Cast() returns true
        protected void ExpendBlood()
        {
            ParentObject.GetPart<XRL.World.Parts.VampireBloodMetabolism>().Blood -= Cost;
        }


        void VerifyInitialization()
        {
            if (CommandName.IsNullOrEmpty())
                throw new Exception($"CommandName not assigned to {GetType().Name}!");
            if (AbilityMenuName.IsNullOrEmpty())
                throw new Exception($"AbiltiyMenuName not assigned to {GetType().Name}!");
        }
        bool EnoughBlood(string text)
        {
            if (ParentObject.GetIntProperty(Flags.BLOOD_VALUE) > Cost)
                return true;
            else
                return ParentObject.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
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