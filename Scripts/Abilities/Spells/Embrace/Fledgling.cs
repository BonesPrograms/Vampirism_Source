
using System;
using Nexus.Spells;
using XRL.World.AI;
using XRL.World.Effects;
using System.Linq;

namespace XRL.World.Parts
{
    [Serializable]
    public class FledglingVampire : IScribedPart
    {

        GameObject _sireCache;
        public GameObject Sire => _sireCache ??= GameObject.Find(x => x.ID == SireID);
        public string SireID;
        public long TimeOfSiring;
        public bool IsFollowing;
        public FledglingVampire()
        {

        }

        public FledglingVampire(GameObject Sire) : this()
        {
            SireID = Sire.ID;
            TimeOfSiring = The.Game.Turns;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplyProselytize");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplyProselytize")
            {
                if (IsFollowing)
                {
                    UI.Popup.Show($"{ParentObject.t()} is already your follower.");
                    return false;
                }
            }
            return base.FireEvent(E);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID)
                return true;
            if (ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID)
                return IsFollowing;
            if (ID == CanApplyEffectEvent.ID || ID == ApplyEffectEvent.ID)
                return IsFollowing;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{ParentObject.t()} is already your follower.");
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CanApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{ParentObject.t()} is already your follower.");
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeBeginTakeActionEvent E)
        {
            if (!CompanionCore.IsSupported(Sire, ParentObject, 5))
            {
                Dismiss(Sire);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            if (!IsFollowing && IsChildeOf(E.Actor) && !ParentObject.IsHostileTowards(E.Actor) && CompanionCore.NotAlreadyUnderEffect(ParentObject, false))
            {
                E.AddAction("Follow", "follow", "FledglingFollowSire", null, 'd', FireOnActor: false, 0, 0, Override: false, WorksAtDistance: true);
            }
            else if (IsFollowing && IsChildeOf(E.Actor))
            {
                E.AddAction("Dismiss", "dismiss", "DismissFledgling", null, 'd', FireOnActor: false, 0, 0, Override: false, WorksAtDistance: true);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (!IsFollowing && E.Command == "FledglingFollowSire" && E.Item == ParentObject && IsChildeOf(E.Actor) && !ParentObject.IsHostileTowards(E.Actor))
            {
                IsFollowing = true;
                CompanionCore.Ally<AllyFledglingChilde>(ParentObject, E.Actor, "Sire", $"{ParentObject.t()} bows before you.", 5);
                CompanionCore.AllyOpinion<OpinionFledglingChilde>(ParentObject, E.Actor);
                E.RequestInterfaceExit();
            }
            if (IsFollowing && E.Command == "DismissFledgling" && E.Item == ParentObject && IsChildeOf(E.Actor))
            {
                Dismiss(E.Actor);
                E.RequestInterfaceExit();
            }
            return base.HandleEvent(E);
        }

        void Dismiss(GameObject Actor)
        {
            IsFollowing = false;
            CompanionCore.Dismiss<AllyFledglingChilde>(Actor, ParentObject, $"You dismiss {ParentObject.t()}");
            CompanionCore.DismissOpinion<OpinionFledglingChilde>(ParentObject, Actor);
            CompanionCore.SyncTarget(Actor, "Sire", 5);
            var badkey = Actor.Brain.PartyMembers.FirstOrDefault(x => x.Value.Reference.Object.ID == ParentObject.ID);
            Actor.Brain.PartyMembers.Remove(badkey.Key);
            ParentObject.Brain.PartyLeader = null;
        }
        public bool IsChildeOf(GameObject Target)
        {
            if (Target.ID == SireID)
            {
                _sireCache = Target;
                return true;
            }
            return false;
        }

    }

}

namespace XRL.World.AI
{

    [Serializable]
    public class AllyFledglingChilde : AllyProselytize
    {
        public override string GetText(GameObject Actor)
        {
            return "I am a childe to " + Name + ".";
        }
    }

    [Serializable]
    public class OpinionFledglingChilde : OpinionProselytize
    {
        public override string GetText(GameObject Actor)
        {
            return "Embraced me.";
        }
    }

}