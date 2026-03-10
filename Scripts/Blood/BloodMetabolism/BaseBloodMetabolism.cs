using XRL.World.Parts;
using XRL.World;
using XRL.World.Effects;
using System.Text;
using System.Linq;
using XRL.UI;


namespace Nexus.Blood
{

    public interface IBloodMetabolism
    {
        public int Blood { get; set; }
        public static void Vomit(GameObject Object)
        {
            StringBuilder MessageHolder = new();
            if (Object.IsPlayer())
                Popup.Show("You vomit {{R sequence|blood!}}");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(Object, ref ExitInterface, MessageHolder);
        }
    }
    public abstract class BaseBloodMetabolism<T> where T : IComponent<GameObject>, IBloodMetabolism
    {
        public T Source { get; protected set; } //will be readonly later but im busy rn and cant write the new base constructor
        public GameObject Metaboliser { get; protected set; }
        public Stomach Stomach => _Stomach ??= Metaboliser.GetPart<Stomach>();
        Stomach _Stomach;
        public bool Glut => Source.Blood >= Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Quenched => Source.Blood >= Rules.Vitae.BLOOD_QUENCHED && Source.Blood < Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Thirsty => Source.Blood >= Rules.Vitae.BLOOD_THIRSTY && Source.Blood < Rules.Vitae.BLOOD_QUENCHED;
        public bool Parched => Source.Blood >= Rules.Vitae.BLOOD_PARCHED && Source.Blood < Rules.Vitae.BLOOD_THIRSTY;
        public bool Min => Source.Blood < Rules.Vitae.BLOOD_PARCHED;
        const int WATER = 35000;
        static readonly string[] _vomitStrings = { "You vomit!", "You vomit {{R sequence|blood!}}" };
        protected enum BloodLevel //so many different ways to track blood... boolean, string, enum, integer - choose your favorite!
        {
            MIN,
            PARCHED,
            THIRSTY,
            QUENCHED,
            GLUT
        }
        public BaseBloodMetabolism(T Source)
        {
            this.Source = Source;
            this.Metaboliser = Source.GetBasisGameObject();
        }
        public abstract void Cycle(); //in the event that its ever stored polymorphically as BaseBloodMetabolism<T>
        public void VomitEventHandler(StringBuilder MessageHolder)
        {
            ShowStrings(MessageHolder);
            if (Metaboliser.CurrentCell != null && !Metaboliser.OnWorldMap())
            {
                FindVomitPool(Metaboliser.CurrentCell);
                CreateVomitObjects();
            }
        }
        public void WaterEvents(Event E, bool CheckPlayer = false)
        {
            if (E.ID == "AfterDrank")
                SetWater();
            if (E.ID == "AddWater")
            {
                int water = E.GetIntParameter("Amount");
                if (water < 0)
                {
                    if (CheckPlayer)
                    {
                        if (Metaboliser.IsPlayer())
                            Source.Blood += water;
                    }
                    else
                        Source.Blood += water;
                }
            }
        }
        public Stomach SetWater()
        {
            Stomach s = Metaboliser.GetPart<Stomach>();
            s.Water = WATER;
            return s;
        }
        protected void Vomit() => IBloodMetabolism.Vomit(Metaboliser);
        protected string StatusToString(out BloodLevel bloodLevel)
        {
            if (Glut)
            {
                bloodLevel = BloodLevel.GLUT;
                return nameof(Glut);
            }
            if (Quenched)
            {
                bloodLevel = BloodLevel.QUENCHED;
                return nameof(Quenched);
            }
            if (Thirsty)
            {
                bloodLevel = BloodLevel.THIRSTY;
                return nameof(Thirsty);
            }
            if (Parched)
            {
                bloodLevel = BloodLevel.PARCHED;
                return nameof(Parched);
            }
            if (Min)
            {
                bloodLevel = BloodLevel.MIN;
                return nameof(Min);
            }
            bloodLevel = default;
            return OutOfRange();
        }
        protected static string OutOfRange()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ BloodMetabolism.TurnBoolToString() -- all values returned false, should not be possible. Will break bloodthirst.");
            return "Error";
        }

        protected bool NotAtMinimum()
        {
            Source.Blood = Source.Blood <= Rules.Vitae.BLOOD_MIN ? Rules.Vitae.BLOOD_MIN : Source.Blood;
            return Source.Blood > Rules.Vitae.BLOOD_MIN;
        }

        void ShowStrings(StringBuilder MessageHolder)
        {
            if (Metaboliser.IsPlayer())
                MessageHolder.Replace(_vomitStrings[0], _vomitStrings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{Metaboliser.t()} vomits" + " {{R|blood!}}");
        }

        void CreateVomitObjects()
        {
            Metaboliser.CurrentCell.AddObject("BloodVomitPool");
            if (Metaboliser.TryGetEffect<LiquidCovered>(out var e))
            {
                e.Liquid.ComponentLiquids.Remove("putrid");
                e.Liquid.ComponentLiquids["blood"] = 2; //was getting a terrible error if the key already existed, dont use .Add!
            }
            else
            {
                LiquidCovered E = new("blood", 2);
                Metaboliser.ApplyEffect(E);
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