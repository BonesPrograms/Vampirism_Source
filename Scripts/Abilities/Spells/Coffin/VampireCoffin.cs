using System;
using Nexus.Spells;

namespace XRL.World.Parts
{

    [Serializable]
    public class VampireCoffin : Bed
    {
        public string OwnerID;
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == TookDamageEvent.ID || ID == InventoryActionEvent.ID || ID == CommandSmartUseEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
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
        public override bool HandleEvent(TookDamageEvent E)
        {
            Cell cell = ParentObject.CurrentZone?.GetEmptyCells()?.GetRandomElement();
            if (cell != null && !E.Damage.Attributes.Contains("Fire") && SpellCore.RealityCheck(cell, ParentObject, VampiricSpell.CLASS, this))
            {
                ParentObject.ParticleBlip("&C\u000f", 10, 0L);
                ParentObject.TeleportSwirl();
                ParentObject.TeleportTo(cell);
                E.Damage.Amount = 0;
                UpdateXY();
                return false;
            }
            return base.HandleEvent(E);
        }

        void UpdateXY()
        {
            GameObject obj = GameObject.FindByID(OwnerID);
            var part = obj.GetPart<CoffinSpell>();
            part.CellX = ParentObject.CurrentCell.X;
            part.CellY = ParentObject.CurrentCell.Y;
        }

        bool Failed(bool player)
        {
            if (player)
                UI.Popup.Show($"{ParentObject.t()} will not open for you.");
            return false;
        }
    }
}