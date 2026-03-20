using System;
using BeastScribe;

namespace XRL.World.Parts
{

    [Serializable]
    public class VampireCoffin : Bed
    {

        [NonSerialized]
        GameObjectReference _ownerCache;

        /// <summary>
        /// Potentially null value 
        /// </summary>
        GameObject Owner
        {
            get
            {
                _ownerCache ??= GameObject.FindByID(OwnerID).Reference();
                return _ownerCache?.Object;
            }
            set
            {
                _ownerCache = value.Reference();
            }
            
        }
        public string OwnerID { get => _ownerID; private init { _ownerID = value; } }
        string _ownerID;

        public VampireCoffin()
        {

        }

        public VampireCoffin(GameObject Object)
        {
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
            var part = Owner?.GetPart<CoffinSpell>();
            part?.CoffinDestroyed();
            ParentObject.ParticleBlip("&R\u000f", 10, 0L);
            AddPlayerMessage($"{ParentObject.t()} vanishes!");
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
                if (cell != null && BaseVampireSpell.RealityCheck(cell, BaseVampireSpell.CATEGORY, this))
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
            var part = Owner?.GetPart<CoffinSpell>();
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

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Scribe.Writer.Scribe(Writer, this);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Scribe.Reader.Scribe(Reader, this);
        }
    }
}