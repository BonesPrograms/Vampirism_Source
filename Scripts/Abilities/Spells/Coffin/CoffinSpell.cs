using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;

using SerializeField = UnityEngine.SerializeField;
using System.Linq;
using XRL.World.Parts.Mutation;
namespace XRL.World.Parts
{

    [Serializable]
    public class CoffinSpell : BaseVampireSpell
    {
        public GameObject Coffin;
        public override int Cooldown => Nexus.Rules.Coffin.MATERIALIZE_COOLDOWN;
        public int JauntCooldown;
        public int Timer;
        public bool CoolingOff;
        public bool HasCoffin;
        public string Zone;
        public int CellX;
        public int CellY;
        bool _justJaunted;
        bool _tookFireDamage;
        public static bool ShowDebug;

        public CoffinSpell()
        {
            AbilityMenuName = Nexus.Rules.Coffin.ABILITY_NAME;
            CommandName = Nexus.Rules.Coffin.COMMAND_NAME;
        }
        public override int Roll() => WikiRng.Next(1, 20) + Level;
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

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.WriteGameObject(Coffin);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Coffin = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == BeforeDieEvent.ID || ID == BeforeTookDamageEvent.ID)
                return !CoolingOff && HasCoffin;
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return _justJaunted || CoolingOff;
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
            if (_justJaunted)
                Jaunted();
            if (CoolingOff)
                CoolOff();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (E.Dying == ParentObject && !_tookFireDamage && !Vampirism.SunlightInterference(ParentObject))
            {
                if ((Roll() >= Nexus.Rules.Coffin.SAVING_THROW_DC) || UI.Options.GetOptionBool(ModOptions.COFFIN))
                {
                    if (RealityCheck(base.ParentObject.CurrentCell))
                    {
                        base.ParentObject.RestorePristineHealth();
                        ActivateCoffin(out var cell);
                        E.Dying.TeleportTo(cell);
                        E.Dying.TeleportSwirl(null, "&C", Voluntary: true, null, 'ù', IsOut: true);
                        E.RequestInterfaceExit();
                        _justJaunted = true;
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
                _tookFireDamage = E.Damage.Attributes.Contains("Fire");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Nexus.Rules.Coffin.COMMAND_NAME && Checks.Prerequisites(base.ParentObject, Nexus.Rules.Coffin.ABILITY_NAME, "invoke your coffin"))
            {
                Cell cell = base.ParentObject.PickDirection(Nexus.Rules.Coffin.ABILITY_NAME);
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
            GameObject newObject = GameObject.Create(Nexus.Rules.Coffin.BLUEPRINT);
            VampireCoffin part = new(ParentObject);
            newObject.AddPart(part);
            Coffin = newObject;
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
            if (ParentObject.TryGetEffect<FrenzyAI>(out var fx))
                StopFrenzy(fx);
            ParentObject.TeleportSwirl(null, "&C", Voluntary: true);
            if (ParentObject.IsPlayer())
                UI.Popup.Show("You return to your coffin!");
            else
                AddPlayerMessage($"{ParentObject.t()} vanishes!");
            _justJaunted = false;
            CoolingOff = true;
            Timer = 0;
            JauntCooldown = WikiRng.Next(Nexus.Rules.Coffin.SAVE_FROM_DEATH_MIN, Nexus.Rules.Coffin.SAVE_FROM_DEATH_MAX);
            ParentObject.ApplyEffect(new Asleep(null, WikiRng.Next(200, 500), true, false, false, true));
        }

        void StopFrenzy(FrenzyAI fx)
        {
            fx.Source.TargetRegistry = new();
            fx.Duration = 0;
        }

        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("Save-From-Death Cooldown", JauntCooldown - Timer, true);
            stats.Set("SaveAndChance", Chance(), true);
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Nexus.Rules.Coffin.MATERIALIZE_COOLDOWN);

            string Chance()
            {
                if (UI.Options.GetOptionBool(ModOptions.COFFIN))
                    return "You will always return to your coffin when Save-From-Death is off cooldown.";
                else
                    return $"Save-From-Death roll: 1d20 + {Level} versus {Nexus.Rules.Coffin.SAVING_THROW_DC}";
            }
        }
    }
}


//NOTES ON ACTIVATECOFFIN(OUT CELL CELL):

//I noticed a few bugs during developemnt. However, these bugs are kindof bugfoot: they come and go. I havent noticed them in recent testing, but here they are:

//coffin goes null if you leave the zone, and if it doesnt go null, its blueprint changes
//coffins must be a single persistent object, so we pretty much need to find the coffin by zone, recreate it entirely, and re-assign it to Coffin
//however during this process, Coffin.CurrentCell == null, so we need to get it's cell by XY
//so that we can remove it (because using TeleportTo doesnt actually remove it and instead duplicates)

//i have a feeling this could be causing hidden issues... if you try to get the coffin by bp it doesnt work always, but getting it by part does work
//it may be accidentally stealing world objects...

//Currently, we are not accidentally stealing world objects, and I have verified that the coffin retain's its proper blueprint, even to the extent that the coffin does not
// consistently go null when leaving the zone. Those strange situations where the blueprint changed (this happened multiple times) has not occured recently.