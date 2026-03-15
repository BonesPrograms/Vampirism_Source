using System;
using XRL.World.Effects;
using VampirismSys.Core;
using VampirismSys.Rules;
using System.Linq;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{

    [Serializable]
    public class CoffinSpell : BaseVampireSpell
    {
        [NonSerialized]
        GameObject Coffin;
        protected override int Cooldown => VampirismSys.Rules.Coffin.MATERIALIZE_COOLDOWN;
        bool CoolingOff => JauntCooldown > 0;
        int JauntCooldown = 0;
        int CooldownTimer = 0;
        bool HasCoffin = false; //i had at least 5 nullref deserialization errors with this type, so all values are initialized to avoid them
        string Zone = string.Empty;
        int CellX = default;
        int CellY = default;

        [NonSerialized]
        bool JustJaunted;

        [NonSerialized]
        bool TookFireDamage;
        internal static bool ShowDebug;
        public CoffinSpell()
        {
            AbilityMenuName = VampirismSys.Rules.Coffin.ABILITY_NAME;
            CommandName = VampirismSys.Rules.Coffin.COMMAND_NAME;
        }
        protected override int Roll() => WikiRng.Next(1, 20) + Level;
        //uses vampirism level like all spells

        public bool UpdateXY(Cell cell)
        {
            if (cell != null)
            {
                CellX = cell.X;
                CellY = cell.Y;
                return true;
            }
            return CoffinDestroyed();
        }

        public bool CoffinDestroyed()
        {
            UI.Popup.Show("You feel your coffin being destroyed.");
            HasCoffin = false;
            return false;
        }


        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == BeforeDieEvent.ID || ID == BeforeTookDamageEvent.ID)
                return !CoolingOff && HasCoffin;
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return JustJaunted || CoolingOff;
            if (ID == AfterDieEvent.ID)
                return HasCoffin;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(AfterDieEvent E)
        {
            if (E.Dying == ParentObject)
            {
                ActivateCoffin(out var cell);
                if (Coffin != null)
                {
                    if (The.Player.HasLOSTo(cell, false))
                    {
                        Coffin.ParticleBlip("&R\u000f", 10, 0L);
                        AddPlayerMessage($"{Coffin.t()} vanishes!");
                    }
                    Coffin.Obliterate();
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (JustJaunted)
                Jaunted();
            if (CoolingOff)
                CoolOff();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (E.Dying == ParentObject && !TookFireDamage && !Vampirism.SunlightInterference(ParentObject))
            {
                if ((Roll() >= VampirismSys.Rules.Coffin.SAVING_THROW_DC) || UI.Options.GetOptionBool(ModOptions.COFFIN))
                {
                    if (RealityCheck(base.ParentObject.CurrentCell))
                    {
                        base.ParentObject.RestorePristineHealth();
                        ActivateCoffin(out var cell);
                        E.Dying.TeleportTo(cell);
                        E.Dying.TeleportSwirl(null, "&C", Voluntary: true, null, 'ù', IsOut: true);
                        E.RequestInterfaceExit();
                        JustJaunted = true;
                        return false;
                    }
                }

            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeTookDamageEvent E)
        {
            if (E.Object == ParentObject && UI.Options.GetOptionBool(ModOptions.FIRE))
            {
                TookFireDamage = E.Damage.Attributes.Contains("Fire");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == VampirismSys.Rules.Coffin.COMMAND_NAME && Checks.Prerequisites(base.ParentObject, VampirismSys.Rules.Coffin.ABILITY_NAME, "invoke your coffin"))
            {
                Cell cell = base.ParentObject.PickDirection(VampirismSys.Rules.Coffin.ABILITY_NAME);
                if (cell != null)
                {
                    if (cell.IsOpenForPlacement())
                    {
                        if (!base.ParentObject.IsRealityDistortionUsable())
                            RealityStabilized.ShowGenericInterdictMessage(base.ParentObject);
                        else
                            Cast(cell);
                    }
                    else
                        base.ParentObject.ShowFailure("You can't invoke your coffin there.");
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

        void ActivateCoffin(out Cell cell) //WIERD BUGS: SEE END OF FILE
        {
            Zone zone = The.ZoneManager.GetZone(Zone);
            cell = zone.Map[CellX][CellY]; //i used to do a cell != null and zone != null check here, but i actually want this to fail very loudly, a silent failure on BeforeDieEvent would not be helpful
            if (Coffin == null || !GameObject.Validate(ref Coffin))
            {
                var obj = cell.Objects.FirstOrDefault(x => x.GetPart<VampireCoffin>()?.OwnerID == ParentObject.ID);
                if (ShowDebug)
                    UI.Popup.Show($"{obj}, {obj?.Blueprint}");
                Coffin = obj;
            }
        }

        void MakeCoffin()
        {
            Coffin = GameObject.Create(VampirismSys.Rules.Coffin.BLUEPRINT);
            Coffin.AddPart(new VampireCoffin(ParentObject));
            Coffin.SetIntProperty("DroppedByPlayer", 1);
        }

        void PlaceCoffin(Cell cell)
        {
            CheckExistence();
            //cell.AddObject(Coffin);
            Coffin.DirectMoveTo(cell);
            CellX = cell.X;
            CellY = cell.Y;
            Zone = cell.ParentZone.DebugName;
            Coffin.ParticleBlip("&R\u000f", 10, 0L);
            AddPlayerMessage($"{Coffin.t()} appears!");
        }


        void CheckExistence()
        {
            if (HasCoffin)
            {
                ActivateCoffin(out _);
            }
            else
            {
                HasCoffin = true;
                MakeCoffin();
            }

        }
        void CoolOff()
        {
            CooldownTimer++;
            if (CooldownTimer >= JauntCooldown)
            {
                CooldownTimer = default;
                JauntCooldown = default;
            }
        }

        void Jaunted()
        {
            if (ParentObject.TryGetEffect<FrenzyAI>(out var fx))
                StopFrenzy(fx);
            ParentObject.TeleportSwirl(null, "&C", Voluntary: true);
            if (ParentObject.IsPlayer())
                UI.Popup.Show("You return to your coffin!");
            else
                AddPlayerMessage($"{ParentObject.t()} vanishes!");
            JustJaunted = false;
            CooldownTimer = 0;
            JauntCooldown = WikiRng.Next(VampirismSys.Rules.Coffin.SAVE_FROM_DEATH_MIN, VampirismSys.Rules.Coffin.SAVE_FROM_DEATH_MAX);
            ParentObject.ApplyEffect(new Asleep(null, WikiRng.Next(200, 500), true, false, false, true));
        }

        void StopFrenzy(FrenzyAI fx)
        {
            fx.Source.TargetRegistry = new();
            fx.Duration = 0;
        }

        protected override void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("Save-From-Death Cooldown", JauntCooldown - CooldownTimer, true);
            stats.Set("SaveAndChance", Chance(), true);
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), VampirismSys.Rules.Coffin.MATERIALIZE_COOLDOWN);

            string Chance()
            {
                if (UI.Options.GetOptionBool(ModOptions.COFFIN))
                    return "You will always return to your coffin when Save-From-Death is off cooldown.";
                else
                    return $"Save-From-Death roll: 1d20 + {Level} versus {VampirismSys.Rules.Coffin.SAVING_THROW_DC}";
            }
        }
    }
}
