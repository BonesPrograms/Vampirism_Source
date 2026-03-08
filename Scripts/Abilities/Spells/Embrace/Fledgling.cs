
using System;
using Nexus.Spells;
using XRL.World.AI;

namespace XRL.World.Parts
{
    [Serializable]
    public class FledglingVampire : IScribedPart
    {

        GameObject _sireCache;
        public GameObject SireCache => _sireCache ??= GameObject.Find(x => x.ID == SireID);
        public string SireID;
        public long TimeOfSiring;
        public bool HatesSire;
        public bool IsFollowing;
        public FledglingVampire()
        {

        }

        public FledglingVampire(GameObject Sire, bool HatesSire) : this()
        {
            SireID = Sire.ID;
            TimeOfSiring = The.Game.Turns;
            this.HatesSire = HatesSire;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID || ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID)
                return true;
            if (ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID)
                return IsFollowing;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(BeforeBeginTakeActionEvent E)
        {
            if (!MasterCore.IsSupported(SireCache, ParentObject, 5))
            {
                Dismiss(SireCache);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            if (!IsFollowing && IsChildeOf(E.Actor) && !HatesSire)
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
            if (!IsFollowing && E.Command == "FledglingFollowSire" && E.Item == ParentObject && IsChildeOf(E.Actor) && !HatesSire)
            {
                IsFollowing = true;
                MasterCore.Ally<AllyProselytize>(ParentObject, E.Actor, "Sire", "You command your fledgling to join you.", 5);
                MasterCore.AllyOpinion<OpinionProselytize>(ParentObject, E.Actor);
                AddPlayerMessage($"{ParentObject.t()} bows and joins you.");
            }
            if (IsFollowing && E.Command == "DismissFledgling" && E.Item == ParentObject && IsChildeOf(E.Actor))
            {
                Dismiss(E.Actor);
            }
            return base.HandleEvent(E);
        }

        void Dismiss(GameObject Actor)
        {
            IsFollowing = false;
            MasterCore.Dismiss<AllyProselytize>(Actor, ParentObject, $"You dismiss {ParentObject.t()}");
            MasterCore.DismissOpinion<OpinionProselytize>(ParentObject, Actor);
            MasterCore.SyncTarget(Actor, "Sire", 5);
        }
        public bool IsChildeOf(GameObject Target)
        {
            return Target.ID == SireID;
        }

    }

}

