
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
using XRL.World.AI;
using System.Linq;

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class VampiricSpell : IScribedPart
    {
        public Guid SpellID = Guid.Empty;
        public const string CATEGORY = "Blood Magic";
        public abstract string CommandName { get; }
        public abstract string AbilityMenuName { get; }
        public abstract int Cooldown { get; }
        public int Level => ParentObject.GetPart<Vampirism>().Level;
        public virtual int Cost => Nexus.Rules.Vitae.BLOOD_PER_SIP; //default 10k  
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

        public virtual void AddSpell()
        {
            SpellID = AddMyActivatedAbility(AbilityMenuName, CommandName, CATEGORY, null, "\u009f");
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

        public abstract string CommandName { get; }
        public abstract string AbilityMenuName { get; }
        public const string CATEGORY = VampiricSpell.CATEGORY;
        public Guid SpellID = Guid.Empty;
        public virtual int Cost => Nexus.Rules.Vitae.BLOOD_PER_SIP; //default 10k 
        public override bool WantEvent(int ID, int Cascade) //current use of FX is very temporary so there is no need for CollecStats or AbilityManager stuff
        {
            if (ID == PooledEvent<CommandEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, Object, CATEGORY, this);
        public void ExpendBlood() => SpellCore.ExpendBlood(Object, Cost);
        public bool Cast(string toDo) => SpellCore.Cast(toDo, Object, this, SpellID, default, Cost, CATEGORY, ClassName);
        public virtual void AddFXSpell()
        {
            SpellID = AddMyActivatedAbility(AbilityMenuName, CommandName, CATEGORY, null, "\u0002");
        }
    }
}



namespace Nexus.Spells
{
    public static class SpellCore
    {
        public static int Roll(GameObject gameObj, int level) => WikiRng.Next(1, 8) + Math.Max(gameObj.StatMod("Ego"), level) + gameObj.GetStat("Level").Value;
        public static bool EnoughBlood(string text, GameObject parentObj, int cost)
        {
            if (parentObj.GetIntProperty(Flags.BLOOD_VALUE) > cost)
                return true;
            else
                return parentObj.ShowFailure("You don't have enough {{R|blood}} " + text + "!");
        }
        public static bool SunlightInterference(GameObject parentObject)
        {
            if (Options.GetOptionBool(ModOptions.NIGHTBEAST))
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
            ParentObject.GetPart<XRL.World.Parts.Vitae>().Blood -= Cost;
        }


    }
    public static class MasterCore
    {
        public static void SyncTarget(GameObject Beguiler, string means, int mask, GameObject Target = null)
        {
            if (Beguiler.Brain == null)
            {
                return;
            }
            int num = GetCompanionLimitEvent.GetFor(Beguiler, means);
            if (Target == null)
            {
                num++;
            }
            XRL.World.AI.PartyCollection partyMembers = Beguiler.Brain.PartyMembers;
            int[] array = (from x in partyMembers
                           where x.Value.Flags.HasBit(mask)
                           orderby Brain.PartyMemberOrder(x) descending
                           select x.Key).ToArray();
            int num2 = 0;
            for (int num3 = array.Length; num3 >= num; num3--)
            {
                partyMembers.Remove(array[num2]);
                num2++;
            }
            if (Target != null)
            {
                partyMembers[Target] = mask;
            }
        }

        public static void Ally<T>(GameObject Object, GameObject Master, string Means, string text, int mask) where T : IAllyReasonSourced, new()
        {
            Object.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_charm");
            IComponent<GameObject>.AddPlayerMessage(text);
            Object.Heartspray();
            MasterCore.SyncTarget(Master, Means, mask, Object);
            Object.SetAlliedLeader<T>(Master);
        }

        public static void Dismiss<T>(GameObject Master, GameObject Object, string text) where T : IAllyReasonSourced
        {
            if (GameObject.Validate(ref Master) && Object.PartyLeader == Master && !Master.SupportsFollower(Object, 13))
            {
                Object.Brain.PartyLeader = null;
                Object.Brain.Goals.Clear();
                if (Object.InSameZone(Master?.CurrentCell))
                    IComponent<GameObject>.AddPlayerMessage(text);
            }
            Object.Brain.RemoveAllegiance<T>(Master?.BaseID ?? 0);
        }

        public static void AllyOpinion<T>(GameObject Object, GameObject Master) where T : IOpinionSubject, new()
        {
            if (Object.Brain != null && GameObject.Validate(ref Master))
                Object.Brain.AddOpinion<T>(Master);
        }

        public static void DismissOpinion<T>(GameObject Object, GameObject Master) where T : IOpinionSubject
        {
            if (Object.Brain != null && GameObject.Validate(ref Master))
                Object.Brain.RemoveOpinion<T>(Master);
        }

        public static bool IsSupported(GameObject Master, GameObject Object, int mask)
        {
            if (GameObject.Validate(ref Master) || !Master.HasHitpoints())
                return Master.SupportsFollower(Object, mask);
            return false;
        }
    }
}


