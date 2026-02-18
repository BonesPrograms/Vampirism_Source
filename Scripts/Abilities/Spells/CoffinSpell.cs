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

        public GameObject Coffin => _Coffin?.Object;
        public GameObjectReference _Coffin;
        public override Type SpellType => typeof(CoffinSpell);
        public override int Cooldown => COFFIN.MATERIALIZE_COOLDOWN;
        bool JustJaunted;
        public int JauntCooldown;
        public int Timer;
        public bool CoolingOff;
        public bool HasCoffin;
        public override int Roll() => WikiRng.Next(1, 20) + Level;
        //uses vampirism level like all spells
        public override void AddSpell()
        {
            SpellID = AddMyActivatedAbility(COFFIN.ABILITY_NAME, COFFIN.COMMAND_NAME, $"{CLASS}", null, "\u009f");
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == TookDamageEvent.ID && !CoolingOff && HasCoffin)
                return true;
            if (ID == SingletonEvent<BeforeTakeActionEvent>.ID && (JustJaunted || CoolingOff || HasCoffin))
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

        void msg(string text) => UI.Popup.Show(text);
        public override bool HandleEvent(TookDamageEvent E)
        {
            msg("TDE");
            if (E.Object == ParentObject)
            {
                if (!E.Damage.Attributes.Contains("Fire") && !SunlightInterference()) // explosions too maybe light
                {
                    msg("NoInterf");
                    msg($"{ParentObject.hitpoints - E.Damage.Amount <= 0} dead?");
                    if (ParentObject.hitpoints - E.Damage.Amount <= 0 && (Roll() >= COFFIN.SAVING_THROW_DC || UI.Options.GetOptionBool(OPTIONS.COFFIN)))
                    {
                        msg("Through");
                        if (RealityCheck(Coffin.CurrentCell) && !Coffin.IsBroken())
                        {
                            E.Damage.Amount = 0;
                            ParentObject.TeleportSwirl(null, "&C", Voluntary: true, null, 'ù', IsOut: true);
                            ParentObject.TeleportTo(Coffin.CurrentCell);
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
            if (E.Command == COFFIN.COMMAND_NAME && Checks.Prerequisites(ParentObject, COFFIN.ABILITY_NAME, "invoke your coffin"))
            {
                Cell cell = ParentObject.PickDirection(COFFIN.ABILITY_NAME);
                if (cell != null)
                {
                    if (cell.IsOpenForPlacement())
                    {
                        if (!ParentObject.IsRealityDistortionUsable())
                            RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                        else
                            Cast(cell);
                    }
                    else
                        ParentObject.ShowFailure("You can't invoke your coffin there.");
                }
            }
            return base.HandleEvent(E);

        }
        void Cast(Cell cell)
        {
            if (base.Cast("to invoke your coffin"))
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
                Coffin.TeleportTo(cell);
            else
            {
                GameObject Coffin = GameObject.Create(COFFIN.BLUEPRINT);
                Coffin.SetStringProperty(FLAGS.COFFIN, ParentObject.ID);
                cell.AddObject(Coffin);
                this._Coffin = Coffin.Reference();
            }
            Coffin.ParticleBlip("&C\u000f", 10, 0L);
            Coffin.TeleportSwirl(null, "&C", Voluntary: true);
            AddPlayerMessage("Your coffin appears!");
        }

        void CheckCoffin()
        {
            if (Coffin?.Blueprint != COFFIN.BLUEPRINT)
                _Coffin = null;
            if (Coffin == null)
            {
                UI.Popup.Show("You feel your coffin being destroyed!");
                HasCoffin = false;
            }
        }
        void CoolOff()
        {
            Timer++;
            if (Timer >= JauntCooldown)
            {
                CoolingOff = false;
                Timer = default;
                JauntCooldown = default;
            }
        }

        void Jaunted()
        {
            ParentObject.TeleportSwirl(null, "&C", Voluntary: true);
            UI.Popup.Show("You return to your coffin!");
            JustJaunted = false;
            CoolingOff = true;
            Timer = 0;
            JauntCooldown = WikiRng.Next(COFFIN.SAVE_FROM_DEATH_MIN, COFFIN.SAVE_FROM_DEATH_MAX);
            ParentObject.ApplyEffect(new Asleep(Coffin, WikiRng.Next(200, 500), true, false, false, true));
        }


        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("Save-From-Death Cooldown", JauntCooldown - Timer, true);
            stats.Set("SaveAndChance", Chance(), true);
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), COFFIN.MATERIALIZE_COOLDOWN);

            string Chance()
            {
                if (UI.Options.GetOptionBool(OPTIONS.COFFIN))
                    return "You will always return to your coffin when Save-From-Death is off cooldown.";
                else
                    return $"Save-From-Death roll: 1d20 + {Level} versus {COFFIN.SAVING_THROW_DC}";
            }
        }
    }
}