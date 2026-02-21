using System;
using Nexus.Rules;
using Nexus.Properties;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using Nexus.Core;
using XRL.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{

    // [Serializable]
    // public class EmbraceableObjectCopy : IPart
    // {
    //     [NonSerialized]
    //     public GameObject Copy; //figure out how to make this work
    //     public EmbraceableObjectCopy(GameObject Object)
    //     {
    //         Copy = Object.DeepCopy();
    //         Copy.MakeInactive();
    //     }
    //     public override void Write(GameObject Basis, SerializationWriter Writer)
    //     {
    //         Writer.WriteObject(Copy);
    //         base.Write(Basis, Writer);
    //     }

    //     public override void Read(GameObject Basis, SerializationReader Reader)
    //     {
    //         Copy = (GameObject)Reader.ReadObject();
    //         base.Read(Basis, Reader);
    //     }
    // }

    [Serializable]
    public class EmbraceSpell : VampiricSpell
    {
        public override Type SpellType => typeof(EmbraceSpell);
        public override int Cooldown => EMBRACE.COOLDOWN;
        public override void CollectStats(Templates.StatCollector stats)
        {
        }

        public override void AddSpell()
        {
            SpellID = AddMyActivatedAbility(EMBRACE.ABILITY_NAME, EMBRACE.COMMAND_NAME, $"{CLASS}", null, "\u009f");
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Nexus.Rules.EMBRACE.COMMAND_NAME && Nexus.Core.Checks.Prerequisites(ParentObject, EMBRACE.ABILITY_NAME, "embrace"))
            {
                FindEmbraceableObject();
            }
            return base.HandleEvent(E);
        }
        void FindEmbraceableObject()
        {
            Cell cell = ParentObject.PickDirection(EMBRACE.ABILITY_NAME);
            if (cell != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var Object = cell.Objects[i];
                    if (Object.TryGetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, out string result))
                    {
                        CheckEmbraceableObject(Object, result);
                        return; //bug here would list EVERY object in the cell. we just take the first object with the flag. i dont really care if corpses are stacked, the player can deal with that
                    }   //(because the game already has issues with trying to easily/quickly target two objets occupying the same cell)
                }
                UI.Popup.Show("There is nothing there to embrace");
            }
        }

        void CheckEmbraceableObject(GameObject Object, string result)
        {
            if (Object.HasEffect<Embraced>())
            {
                UI.Popup.Show($"{Object.t()} is already being embraced.");
            }
            else if (result == FLAGS.TRUE)
            {
                if (Object.GetIntProperty(FLAGS.EMBRACE.LEVEL_ON_DEATH) < Level + ParentObject.Level)
                {
                    if (!ParentObject.IsRealityDistortionUsable())
                        RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                    else
                        Cast(Object);
                }
                else
                {
                    UI.Popup.Show($"{Object.t()}'s soul is too powerful for you to embrace.");
                }
            }
            else
            {
                UI.Popup.Show($"You cannot embrace {Object.t()}");
            }

        }

        void Cast(GameObject Object)
        {
            if (base.Cast("to embrace"))
            {
                base.ExpendBlood(false, $"You pour your blood down {Object.t()}'s throat.");
                if (RealityCheck(Object.CurrentCell))
                    Embrace(Object);
            }
        }
        void Embrace(GameObject Object)
        {
            // MessageQueue.Suppress = true;
            // var copy = Object.GetPart<EmbraceableObjectCopy>();
            // GameObject obj = copy.Copy;
            // obj.MakeActive();
            // Object.CurrentCell.AddObject(obj);
            // int time = WikiRng.Next(50, 100);
            // obj.ApplyEffect(new Asleep(time, true, false, false, true));
            // obj.ApplyEffect(new Embracing(time, Level));
            // Object.Obliterate();
            // MessageQueue.Suppress = false;
        }


    }



    //plan: we will requirepart by string name
    //we prob wont create an instance from a blueprint, well create an instance of their blueprint to match things like physics and  render and displayname stuff maybe idk. lots of parts to add lowkey.

    // final note: in scan, have it show object level specifically. additionally will need to see mutations w/ levels and skills and cybernetics etc for our copier. 
    // thank god there is a mutation lsit i can past that to one of my loggers. will need otherstuff like bodyparts too - anything listed in Copy. new wish "CheckCopy"

    //NEW ARRAY IDEA:
    //should we tag relations as well? opinions of the player? yes a list of Opinions to the player at least would be valid

    //OTHER ARRAY IDEA:
    //SKILLS string

    //best idea: look into DeepCopy and base it off that kinda...
    [Serializable]

    public class EmbraceableObjectCopy : IPart
    {
        public string DisplayName = default;
        public bool HadMutations = default;
        public bool HadCybernetics = default;
        public int Level = default;
        public string Blueprint = default;
        public (string, string)[] CyberneticAndBodypart = new (string, string)[0];
        public (string, string)[] StringProperties = new (string, string)[0];
        public (string, long)[] LongProperties = new (string, long)[0];
        public (string, int)[] IntProperties = new (string, int)[0];
        public (string, int)[] StatLevels = new (string, int)[0];
        public (string, int)[] MutationsWithLevels = new (string, int)[0]; //cap = mutations.count
        public (string, bool)[] BodyParts = new (string, bool)[0];
        public string[] IParts = new string[0];

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Write(Writer, CyberneticAndBodypart);
            Write(Writer, StringProperties);
            Write(Writer, LongProperties);
            Write(Writer, IntProperties);
            Write(Writer, StatLevels);
            Write(Writer, MutationsWithLevels);
            Write(Writer, BodyParts);
            Write(Writer, IParts);
            base.Write(Basis, Writer);
        }

        void Write(SerializationWriter Writer, IList array)
        {
            // Writer.Write(array.Count);
            for (int i = 0; i < array.Count; i++)
                Writer.WriteObject(array[i]);
        }

        void Read<T>(SerializationReader Reader, IList array)
        {
            // Reader.ReadInt32();
            for (int i = 0; i < array.Count; i++)
            {
                array[i] = (T)Reader.ReadObject();
            }

        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Read<ValueTuple<string, string>>(Reader, CyberneticAndBodypart);
            Read<ValueTuple<string, string>>(Reader, StringProperties);
            Read<ValueTuple<string, long>>(Reader, LongProperties);
            Read<ValueTuple<string, int>>(Reader, IntProperties);
            Read<ValueTuple<string, int>>(Reader, StatLevels);
            Read<ValueTuple<string, int>>(Reader, MutationsWithLevels);
            Read<ValueTuple<string, bool>>(Reader, BodyParts);
            Read<string>(Reader, IParts);
            base.Read(Basis, Reader);
        }
        public (string, IList)[] Arrays => new (string, IList)[]
        {
                 (nameof(MutationsWithLevels), MutationsWithLevels),
                 (nameof(IParts), IParts),
                 (nameof(CyberneticAndBodypart), CyberneticAndBodypart),
                 (nameof(StringProperties), StringProperties),
                 (nameof(IntProperties), IntProperties),
                 (nameof(LongProperties), LongProperties),
                 (nameof(StatLevels), StatLevels),
                 (nameof(BodyParts), BodyParts)
        };

        public (string, object)[] SimpleFields => new (string, object)[]
        {
            (nameof(HadMutations), HadMutations),
            (nameof(HadCybernetics), HadCybernetics),
            (nameof(Level), Level),
            (nameof(Blueprint), Blueprint),
            (nameof(DisplayName), DisplayName)
        };

        public EmbraceableObjectCopy(GameObject Object)
        {
            Copy(Object);
        }
        public void Read()
        {
            MetricsManager.LogInfo($"\nREADING INFO ON {ParentObject.DisplayName}, {ParentObject}, {ParentObject.ID} START");
            ReadArrays();
            MetricsManager.LogInfo("\n ARRAYS FINISHED");
            Read(SimpleFields);
            MetricsManager.LogInfo($"\nREADING INFO ON {ParentObject.DisplayName}, {ParentObject}, {ParentObject.ID} END");
        }
        public void ReadArrays()
        {
            (string, IList)[] Arrays = this.Arrays;
            for (int i = 0; i < Arrays.Length; i++)
            {
                MetricsManager.LogInfo($"\n{Arrays[i].Item1} START");
                CastArray(Arrays[i].Item2);
                MetricsManager.LogInfo($"\n{Arrays[i].Item1} END");
            }
        }
        static void CastArray(IList obj)
        {
            switch (obj)
            {
                case (string, long)[] array0:
                    Read(array0);
                    break;
                case (string, int)[] array1:
                    Read(array1);
                    break;
                case (string, bool)[] array2:
                    Read(array2);
                    break;
                case (string, string)[] array3:
                    Read(array3);
                    break;
                case string[] array4:
                    Read(array4);
                    break;
            }
        }
        static void Read<T1, T2>((T1, T2)[] array)
        {
            for (int i = 0; i < array.Length; i++)
                MetricsManager.LogInfo($"{array[i].Item1}, {array[i].Item2}");
        }
        static void Read<T>(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
                MetricsManager.LogInfo($"{array[i]}");
        }
        void Copy(GameObject Object)
        {
            CopyMutations(Object);
            CopyIParts(Object);
            Blueprint = Object.Blueprint;
            Level = Object.Level;
            DisplayName = Object.DisplayName;
        }

        void CopyIParts(GameObject Object)
        {
            IParts = new string[Object.PartsList.Count];
            for (int i = 0; i < Object.PartsList.Count; i++)
            {
                IParts[i] = Object.PartsList[i].Name;
            }
        }

        void CopyMutations(GameObject Object)
        {
            Mutations m = Object.GetPart<Mutations>();
            if (m != null && m.MutationList.Count > 0)
            {
                MutationsWithLevels = new (string, int)[m.MutationList.Count];
                for (int i = 0; i < m.MutationList.Count; i++)
                {
                    MutationsWithLevels[i].Item1 = m.MutationList[i].Name;
                    MutationsWithLevels[i].Item2 = m.MutationList[i].Level;
                }
                HadMutations = true;

            }
        }
    }

}




