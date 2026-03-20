using XRL.World.Effects;
using System.Text;
using System.Linq;
using XRL.UI;
using VampirismSys.Extensions;
using VampirismSys.Blood;
using VampirismSys.Rules;
using System;
using VampirismSys.Core;

namespace VampirismSys.Blood
{
    public enum BloodLevel : int
    {
        OUT_OF_RANGE,
        MIN,
        PARCHED,
        THIRSTY,
        QUENCHED,
        GLUT
    }
}

namespace XRL.World.Parts
{

    [Serializable]
    public abstract class BaseBloodMetabolism : IBeastScribedPart
    {
        public int Blood
        {
            get => _blood;
            set
            {
                _blood =
                  value < 0 ? 0
                : value > Metab.BLOOD_PUKE ? Metab.BLOOD_PUKE
                : value;
            }
        }
        public BloodLevel Status { get => _status; private set { _status = value; } }
        public string StringStatus { get => _stringStatus; private set { _stringStatus = value; } }
        public virtual string UIBloodDisplay => Status switch
        {
            >= BloodLevel.GLUT => "{{G|Glutted}}",
            >= BloodLevel.QUENCHED => "{{g|Gorged}}",
            >= BloodLevel.THIRSTY => "{{R|Thirsty}}",
            >= BloodLevel.PARCHED => "{{r|Fiending}}",
            _ => "{{r|Ravenous}}"
        };

        protected virtual bool WantsMetabolism => true;

        protected virtual bool WantsVomit => true;

        protected virtual int MetabolismRate => ParentObject.IsVampire() ? VampirismSys.Rules.Metab.BLOOD_METAB : VampirismSys.Rules.Metab.Metab_Settings.DEFAULT;

        public bool Glut => Blood >= VampirismSys.Rules.Metab.BLOOD_GLUTTONOUS;

        public bool Quenched => Blood >= VampirismSys.Rules.Metab.BLOOD_QUENCHED && Blood < VampirismSys.Rules.Metab.BLOOD_GLUTTONOUS;

        public bool Thirsty => Blood >= VampirismSys.Rules.Metab.BLOOD_THIRSTY && Blood < VampirismSys.Rules.Metab.BLOOD_QUENCHED;

        public bool Parched => Blood >= VampirismSys.Rules.Metab.BLOOD_PARCHED && Blood < VampirismSys.Rules.Metab.BLOOD_THIRSTY;

        public bool Min => Blood < VampirismSys.Rules.Metab.BLOOD_PARCHED;

        int _blood = Metab.BLOOD_GLUTTONOUS;

        BloodLevel _status = default;

        BloodLevel _lastStatus = default; //these will also cause deserialization errors

        string _stringStatus = string.Empty;

        const int WATER = 35000;

        static readonly string[] _vomitStrings = { "You vomit!", "You vomit {{R sequence|blood!}}" };

        protected Stomach Stomach => _stomach ??= ParentObject.GetPart<Stomach>();

        [NonSerialized]
        Stomach _stomach;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("AddWater");
            Registrar.Register("AfterDrank");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "AfterDrank")
                SetWater();
            if (E.ID == "AddWater" && WantsMetabolism)
            {
                int water = E.GetIntParameter("Amount");
                if (water < 0)
                    Blood += water;
            }
            return base.FireEvent(E);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == PooledEvent<InduceVomitingEvent>.ID)
                return true;
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(InduceVomitingEvent E)
        {
            if (E.Object == ParentObject)
            {
                SetWater();
                VomitEventHelper(E.MessageHolder);
                if (WantsMetabolism)
                    Blood -= WikiRng.Next(15000, 25000);
                E.InterfaceExit = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            SetWater();
            if (WantsMetabolism)
            {
                Overfed();
                Cycle();
            }
            return base.HandleEvent(E);
        }
        protected virtual void Cycle() //always run last
        {
            Blood -= MetabolismRate;
            SetStatus();
        }
        public void Drink(int value = VampirismSys.Rules.Metab.BLOOD_PER_SIP)
        {
            Blood += value;
            Event E = Event.New("AddFood");
            E.SetParameter("Satiation", "Snack");
            E.SetFlag("Meat", true);
            base.ParentObject.FireEvent(E);
            base.ParentObject.FireEvent(Event.New("AfterDrank")); //for glotrot. all you need is this event and glotrot seems to work intrinsically with putrefying blood
        }

        public void Vomit() //this is a force-vomit invoker for blooddrinkers, if you want them to vomit at a certain threshold, invoke this
        {                                                      //all inheritors of BaseBloodMetabolism<T> have access to an instance version of Vomit(GameObject)
            if (ParentObject.IsPlayer())
                Popup.Show(_vomitStrings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{ParentObject.t()} vomits " + "{{R|blood}}!");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(ParentObject, ref ExitInterface, new StringBuilder());
        }
        protected bool StatusChange(out bool lostBlood, out bool gainedBlood)
        {
            lostBlood = false;
            gainedBlood = false;
            if (_lastStatus == Status)
                return false;
            lostBlood = Status < _lastStatus;
            gainedBlood = Status > _lastStatus;
            _lastStatus = Status;
            return true;
        }

        void Overfed()
        {
            if (Blood >= VampirismSys.Rules.Metab.BLOOD_PUKE && WantsVomit)
            {
                if (ParentObject.IsPlayer())
                    Popup.Show("You overfed!");
                Vomit();
            }
        }

        void SetWater() => Stomach.Water = WATER;

        void VomitEventHelper(StringBuilder MessageHolder)
        {
            ShowVomitStrings(MessageHolder);
            if (ParentObject.CurrentCell != null && !ParentObject.OnWorldMap())
            {
                FindVomitPool(ParentObject.CurrentCell);
                CreateVomitObjects();
            }
        }

        void SetStatus()//so many different ways to track blood... booleans, strings, enums, integers - choose your favorite!
        {
            if (Glut)
            {
                Status = BloodLevel.GLUT;
                StringStatus = nameof(Glut);
            }
            else if (Quenched)
            {
                Status = BloodLevel.QUENCHED;
                StringStatus = nameof(Quenched);
            }
            else if (Thirsty)
            {
                Status = BloodLevel.THIRSTY;
                StringStatus = nameof(Thirsty);
            }
            else if (Parched)
            {
                Status = BloodLevel.PARCHED;
                StringStatus = nameof(Parched);
            }
            else if (Min)
            {
                Status = BloodLevel.MIN;
                StringStatus = nameof(Min);
            }
            else
            {
                Status = default;
                StringStatus = OutOfRange();
            }
        }
        static string OutOfRange()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ BaseBloodMetabolism.SetStatus -- all values returned false, should not be possible. System is broken.");
            return "Error";
        }

        void ShowVomitStrings(StringBuilder MessageHolder)
        {
            if (ParentObject.IsPlayer())
                MessageHolder.Replace(_vomitStrings[0], _vomitStrings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{ParentObject.t()} vomits" + " {{r|blood!}}");
        }

        void CreateVomitObjects()
        {
            ParentObject.CurrentCell.AddObject("BloodVomitPool");
            if (ParentObject.TryGetEffect<LiquidCovered>(out var e))
            {
                e.Liquid.ComponentLiquids.Remove("putrid");
                e.Liquid.ComponentLiquids["blood"] = 2; //was getting a terrible error if the key already existed, dont use .Add!
            }
            else
            {
                LiquidCovered E = new("blood", 2);
                ParentObject.ApplyEffect(E);
                E.Liquid.ComponentLiquids.Remove("putrid");
            }
        }
        static void FindVomitPool(Cell cell)
        {
            var pool = cell.Objects.FirstOrDefault(x => x.Blueprint == "VomitPool");
            if (pool != null)
                cell.RemoveObject(pool);
        }



    }
}