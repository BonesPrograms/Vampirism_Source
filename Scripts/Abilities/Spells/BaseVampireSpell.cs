
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
    public abstract class BaseVampireSpell : IScribedPart
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
        public virtual int Roll()
        {
            return WikiRng.Next(1, 8) + Math.Max(ParentObject.StatMod("Ego"), Level) + ParentObject.GetStat("Level").Value;
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
        public bool RealityCheck(Cell cell) => SpellCore.RealityCheck(cell, ParentObject, CATEGORY, this);

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
    }
}

namespace Nexus.Spells
{
    //mostly based off methods from beguiling/persuasion

    public static class SpellCore
    {
        public static bool RealityCheck<T>(Cell cell, GameObject ParentObject, string category, T Invoker) where T : IPart
        {
            Event E = Event.New("InitiateRealityDistortionTransit", "Object", ParentObject, $"{category}", Invoker, "Cell", cell);
            if (!ParentObject.FireEvent(E) || !ParentObject.CurrentCell.FireEvent(E))
            {
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                return false;
            }
            return true;
        }
    }
    public static class CompanionCore
    {
        public static bool NotAlreadyUnderEffect(GameObject pick, bool showPopup = true) //for now - i have problems with you trying to mix and match these effects
        {
            Effect e = pick.Effects.FirstOrDefault(CheckEffect);
            if (e != null)
            {
                if (showPopup)
                    XRL.UI.Popup.Show($"{pick.t()} is already your follower.");
                return false;
            }
            return true;
        }
        public static bool CheckEffect(Effect e) => e is Beguiled or Proselytized or EnthralledGhoul;
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
            CompanionCore.SyncTarget(Master, Means, mask, Object);
            Object.SetAlliedLeader<T>(Master);
        }

        public static void Dismiss<T>(GameObject Master, GameObject Object, string text, int mask) where T : IAllyReasonSourced
        {
            if (GameObject.Validate(ref Master) && Object.PartyLeader == Master && !Master.SupportsFollower(Object, mask))
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
        public static bool IsSupported(GameObject Master, GameObject Object)
        {
            if (GameObject.Validate(ref Master) || !Master.HasHitpoints())
                return Master.SupportsFollower(Object);
            return false;
        }
    }
}


