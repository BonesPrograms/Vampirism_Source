using XRL.World.Parts;
using XRL.World;
using XRL.World.Effects;
using System.Text;
using System.Linq;
using XRL.UI;
using Nexus.Core;


namespace Nexus.Blood
{


    public enum BloodLevel
    {
        OUT_OF_RANGE,
        MIN,
        PARCHED,
        THIRSTY,
        QUENCHED,
        GLUT
    }
    public interface IBloodMetabolism
    {
        public int Blood { get; set; }
        public static readonly string[] VomitStrings = { "You vomit!", "You vomit {{R sequence|blood!}}" };
        public static void Vomit(GameObject bloodMetaboliser) //this is a force-vomit invoker for blooddrinkers, if you want them to vomit at a certain threshold, invoke this
        {                                                      //all inheritors of BaseBloodMetabolism<T> have access to an instance version of Vomit(GameObject)
            StringBuilder MessageHolder = new();
            if (bloodMetaboliser.IsPlayer())
                Popup.Show(VomitStrings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{bloodMetaboliser.t()} vomits " + "{{R|blood}}!");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(bloodMetaboliser, ref ExitInterface, MessageHolder);
        }
    }
    public abstract class BaseBloodMetabolism<T> where T : IComponent<GameObject>, IBloodMetabolism
    {
        public int Blood //so you don't need to write Source.Blood all the time. its a property that assigns to a property
        {
            get => Source.Blood;
            set
            {
                Source.Blood = value;
            }
        }
        public readonly T Source;
        public readonly GameObject Metaboliser;
        public readonly Stomach Stomach;
        public bool Glut => Blood >= Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Quenched => Blood >= Rules.Vitae.BLOOD_QUENCHED && Blood < Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool Thirsty => Blood >= Rules.Vitae.BLOOD_THIRSTY && Blood < Rules.Vitae.BLOOD_QUENCHED;
        public bool Parched => Blood >= Rules.Vitae.BLOOD_PARCHED && Blood < Rules.Vitae.BLOOD_THIRSTY;
        public bool Min => Blood < Rules.Vitae.BLOOD_PARCHED;
        const int WATER = 35000;
        public BaseBloodMetabolism(T Source)
        {
            this.Source = Source;
            this.Metaboliser = Source.GetBasisGameObject();
            this.Stomach = Metaboliser.GetPart<Stomach>();
        }
        public abstract void Cycle(); //in the event that its ever stored polymorphically as BaseBloodMetabolism<T>
        public void SetWater() => Stomach.Water = WATER;
        protected void Vomit() => IBloodMetabolism.Vomit(Metaboliser);
        public void VomitEventHandler(InduceVomitingEvent E, bool CheckPlayer = false)
        {
            if (E.Object == Metaboliser)
            {
                SetWater();
                VomitEventHandlerHelper(E.MessageHolder);
                if (CheckPlayer)
                {
                    if (Metaboliser.IsPlayer())
                        Blood -= WikiRng.Next(15000, 25000);
                }
                else
                    Blood -= WikiRng.Next(15000, 25000);
                E.InterfaceExit = true;
            }
        }
        public void VomitEventHandlerHelper(StringBuilder MessageHolder)
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
                            Blood += water;
                    }
                    else
                        Blood += water;
                }
            }
        }
        public string StatusToString(out BloodLevel bloodLevel)//so many different ways to track blood... booleans, strings, enums, integers - choose your favorite!
        {                                                       //strings: legacy, what i initially came up with, abstracted value of what your bloodlevel generally is tracked in GameObject.Property as string labels
            if (Glut)                                           //raw integer - legacy, what i initially came up with, you compare the value against my constants for integer blood levels, tracked in GameObject.IntProperty
            {                                                   //enum - super combo: strings dont work for comparison
                bloodLevel = BloodLevel.GLUT;                   //checking if the string "Min" is > than the string "Glut" is not how it should be
                return nameof(Glut);                            //enum combines the generalization of strings with the comparison abilities of integers
            }                                                   //and abstracts those values into a single type
            if (Quenched)                                       //much easier than going to check the constant table of string and integer blood levels
            {                                                   //back then, BloodMetabolism was not a field, a new instance was created every turn as a local variable, 
                bloodLevel = BloodLevel.QUENCHED;               //so it was completely inaccessible and its values could only be accessed through abstraction from it's output: 
                return nameof(Quenched);                        //it wrote the two properties to the GameObject and also wrote to Vitae.Blood and Bloodlusted. 
            }                                                   //but now it is possible for other classes to access BloodMetabolism and directly retrieve a clean enum representation of blood level
            if (Thirsty)                                         // because of the complicated booleans, i dont even think the string property is that useful, and it may be converted to a 1-5 int property of "generalized" level
            {                                                   ///as i intend to keep the properties so that other mods can access them w/o dependency or reflection
                bloodLevel = BloodLevel.THIRSTY;                //the booleans: obviously always important
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
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ BaseBloodMetabolism.StatusToString -- all values returned false, should not be possible. Will break bloodthirst.");
            return "Error";
        }

        protected bool NotAtMinimum()
        {
            Blood = Blood <= Rules.Vitae.BLOOD_MIN ? Rules.Vitae.BLOOD_MIN : Blood;
            return Blood > Rules.Vitae.BLOOD_MIN;
        }

        void ShowStrings(StringBuilder MessageHolder)
        {
            if (Metaboliser.IsPlayer())
                MessageHolder.Replace(IBloodMetabolism.VomitStrings[0], IBloodMetabolism.VomitStrings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{Metaboliser.t()} vomits" + " {{r|blood!}}");
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