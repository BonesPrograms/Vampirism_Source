using System;
using Nexus.Spells;

namespace XRL.World.Parts
{

    [Serializable]
    public class VampireCoffin : Bed
    {
        GameObject _ownerCache;

        /// <summary>
        /// Potentially null value 
        /// </summary>
        public GameObject OwnerCache => _ownerCache ??= GameObject.FindByID(OwnerID);
        public string OwnerID;

        public VampireCoffin()
        {

        }

        public VampireCoffin(GameObject Object)
        {
            _ownerCache = Object;
            OwnerID = Object.ID;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == BeforeApplyDamageEvent.ID || ID == InventoryActionEvent.ID || ID == CommandSmartUseEvent.ID || ID == PooledEvent<IdleQueryEvent>.ID || ID == OnDestroyObjectEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(OnDestroyObjectEvent E)
        {
            var part = OwnerCache?.GetPart<CoffinSpell>();
            part?.CoffinDestroyed();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(IdleQueryEvent E)
        {
            if (E.Actor.ID != OwnerID)
                return false;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandSmartUseEvent E)
        {
            if (E.Actor.ID != OwnerID)
                return Failed(E.Actor.IsPlayer());
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == "SleepOnBed" && E.Actor.ID != OwnerID)
                return Failed(E.Actor.IsPlayer());
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeApplyDamageEvent E)
        {
            if (E.Actor?.ID != OwnerID && !E.Damage.Attributes.Contains("Fire"))
            {
                Cell cell = ParentObject.CurrentZone?.GetEmptyCells()?.GetRandomElement();
                if (cell != null && SpellCore.RealityCheck(cell, ParentObject, VampiricSpell.CATEGORY, this))
                {
                    NotifyTargetImmuneEvent.Send(E.Weapon, E.Object, E.Actor, E.Damage, this);
                    ParentObject.ParticleBlip("&R\u000f", 10, 0L);
                    ParentObject.TeleportSwirl(null, "&C", Voluntary: true);
                    ParentObject.TeleportTo(cell);
                    E.Damage.Amount = 0;
                    UpdateXY();
                    return false;
                }
            }
            return base.HandleEvent(E);
        }

        void UpdateXY()
        {
            var part = OwnerCache?.GetPart<CoffinSpell>();
            if (part?.UpdateXY(ParentObject.CurrentCell) ?? false)
                return;
            ParentObject.Obliterate();
        }

        bool Failed(bool player)
        {
            if (player)
                UI.Popup.Show($"{ParentObject.t()} will not open for you.");
            return false;
        }
    }
}