using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using Nexus.Properties;
namespace XRL.World.Parts
{

    [Serializable]
    public class CoffinSpell : VampiricSpell
    {
        public GameObjectReference Coffin;
        bool JustJaunted;
        public int Cooldown;
        public int Timer;
        public bool CoolingOff;
        public bool HasCoffin;
        public override int Roll() => WikiRng.Next(1, 20) + Level;
        //uses vampirism level like all spells
        public override bool ShouldSync() => true;
        public override void RequireObject()
        {
            ID = AddMyActivatedAbility(COFFIN.ABILITY_NAME, COFFIN.COMMAND_NAME, TAG, null, "\u009f");
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == TookDamageEvent.ID || ID == SingletonEvent<BeforeTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (JustJaunted)
                Jaunted();
            if (CoolingOff)
                CoolOff();
            if (HasCoffin)
                CheckCoffin();
            return base.HandleEvent(E);
        }

        void CheckCoffin()
        {
            if (Coffin.Object?.Blueprint != COFFIN.BLUEPRINT)
                Coffin = null;
            if (Coffin == null)
            {
                UI.Popup.Show("You feel your coffin being destroyed!");
                HasCoffin = false;
            }
        }
        void CoolOff()
        {
            Timer++;
            if (Timer >= Cooldown)
            {
                CoolingOff = false;
                Timer = default;
                Cooldown = default;
            }
        }

        void Jaunted()
        {
            ParentObject.ParticleBlip("&K-", 10, 0L);
            UI.Popup.Show("You return to your coffin!");
            JustJaunted = false;
            CoolingOff = true;
            Timer = 0;
            Cooldown = WikiRng.Next(COFFIN.SAVE_FROM_DEATH_MIN, COFFIN.SAVE_FROM_DEATH_MAX);
            ParentObject.ApplyEffect(new Asleep(Coffin.Object, WikiRng.Next(200, 500), true, false, false, true));
        }
        public override bool HandleEvent(TookDamageEvent E)
        {
            if (E.Object == ParentObject)
            {
                if (!CoolingOff && HasCoffin && !E.Damage.Attributes.Contains("Fire")) // explosions too maybe light
                {
                    if (ParentObject.hitpoints - E.Damage.Amount <= 0 && (Roll() >= COFFIN.SAVING_THROW_DC || UI.Options.GetOptionBool(OPTIONS.COFFIN)))
                    {
                        if (RealityCheck(ParentObject.CurrentCell) && !Coffin.Object.IsBroken())
                        {
                            E.Damage.Amount = 0;
                            ParentObject.DirectMoveTo(Coffin.Object.CurrentCell);
                            JustJaunted = true;
                            return false;
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COFFIN.COMMAND_NAME)
            {
                Cell cell = ParentObject.PickDirection(COFFIN.ABILITY_NAME);
                if (cell != null)
                {
                    if (cell.IsOpenForPlacement())
                        Cast(cell);
                    else
                        ParentObject.ShowFailure("You can't invoke your coffin there.");
                }
            }
            return base.HandleEvent(E);

        }
        void Cast(Cell cell)
        {
            if (base.Cast(COFFIN.ABILITY_NAME, COFFIN.MATERIALIZE_COOLDOWN, "to invoke your coffin"))
            {
                ExpendBlood();
            if (RealityCheck(cell))
                PlaceCoffin(cell);
            }
        }
        
        void PlaceCoffin(Cell cell)
        {
            HasCoffin = true;
            if (Coffin != null)
                Coffin.Object.DirectMoveTo(cell);
            else
            {
                GameObject Coffin = GameObject.Create(COFFIN.BLUEPRINT);
                Coffin.SetStringProperty(FLAGS.COFFIN, ParentObject.ID);
                cell.AddObject(Coffin);
                this.Coffin = Coffin.Reference();
            }
            AddPlayerMessage("Your coffin appears!");
        }

        string Chance()
        {
            if (UI.Options.GetOptionBool(OPTIONS.COFFIN))
                return "You will always return to your coffin when Save-From-Death is off cooldown.";
            else
                return $"Save-From-Death roll: 1d20 + {Level} versus {COFFIN.SAVING_THROW_DC}";
        }


        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("Save-From-Death Cooldown", Cooldown - Timer, true);
            stats.Set("SaveAndChance", Chance(), true);
            stats.CollectCooldownTurns(MyActivatedAbility(ID), COFFIN.MATERIALIZE_COOLDOWN);
        }
    }
}