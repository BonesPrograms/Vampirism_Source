using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{

    [Serializable]
    public class CoffinSpell : VampiricSpell
    {

        public GameObject Coffin;
        public override bool ShouldSync() => true;
        bool Jaunted;
        const string CMD = "invokeCoffin";
        public int thresh;
        public int cooldown;
        public bool used;

        [NonSerialized]
        static public bool AutoWin;
        public override int Roll() => WikiRng.Next(1, 20) + Level;
        //uses vampirism level like all spells

        public override void RequireObject()
        {
            ID = AddMyActivatedAbility("Teleport Coffin", CMD, TAG, null, "\u009f");
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == TookDamageEvent.ID)
                return true;
            if (Jaunted && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
            if (used && ID == SingletonEvent<BeforeTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            cooldown++;
            if (cooldown >= thresh)
            {
                used = false;
                cooldown = default;
                thresh = default;
            }
            return base.HandleEvent(E);

        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            UI.Popup.Show("You return to your coffin!");
            Jaunted = false;
            used = true;
            cooldown = 0;
            thresh = WikiRng.Next(1000, 5000);
            ParentObject.ApplyEffect(new Asleep(Coffin, WikiRng.Next(200, 500), true, false, false, true));
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(TookDamageEvent E)
        {
            if (E.Object == ParentObject)
            {
                if (!used && !E.Damage.Attributes.Contains("Fire")) // explosions too maybe light
                {
                    if (ParentObject.hitpoints - E.Damage.Amount <= 0 && (Roll() >= 20) || AutoWin)
                    {
                        E.Damage.Amount = 0;
                        ParentObject.DirectMoveTo(Coffin.CurrentCell);
                        Jaunted = true;
                        return false;
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == CMD)
            {
                Cell cell = ParentObject.PickDirection("Teleport Coffin");
                if (cell != null)
                {
                    if (cell.IsOpenForPlacement())
                    {
                        ParentObject.UseEnergy(1000, TAG + " Teleport Coffin");
                        CooldownMyActivatedAbility(ID, 1000);
                        if (Coffin != null)
                            Coffin.DirectMoveTo(cell);
                        else
                        {
                            Coffin = GameObject.Create("Bed");
                            cell.AddObject(Coffin);
                        }
                        AddPlayerMessage("Your coffin appears!");

                    }
                    else
                        ParentObject.ShowFailure("You need an empty spot to place your coffin.");
                }
            }
            return base.HandleEvent(E);

        }


        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.CollectCooldownTurns(MyActivatedAbility(ID), 1000);
        }
    }
}