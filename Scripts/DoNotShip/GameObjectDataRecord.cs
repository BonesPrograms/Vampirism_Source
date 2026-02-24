using System;
using System.Linq;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts
{


    //plan: we will requirepart and mutation by string name and .Name
    // we will recreate them from the blueprint then require everything onto the new object
    //dismember limbs that shant be there etc
    //look into DeepCopy to see what we need to copy
    //but first TEST SERIALIZING with only the mutations list for now
    // or partslist thats easy
    //IDEA was to make the arrays not public so that they dont throw deserialize errors
    //we prob wont create an instance from a blueprint, well create an instance of their blueprint to match things like physics and  render and displayname stuff maybe idk. lots of parts to add lowkey.

    // final note: in scan, have it show object level specifically. additionally will need to see mutations w/ levels and skills and cybernetics etc for our copier. 
    // thank god there is a mutation lsit i can past that to one of my loggers. will need otherstuff like bodyparts too - anything listed in Copy. new wish "CheckCopy"

    //NEW ARRAY IDEA:
    //should we tag relations as well? opinions of the player? yes a list of Opinions to the player at least would be valid

    //OTHER ARRAY IDEA:
    //SKILLS string

    //best idea: look into DeepCopy and base it off that kinda...
    [Serializable]

    public class GameObjectDataRecord : IPart
    {
        public string DisplayName = default;
        public string Blueprint = default;
        public string ID = default;
        public int Level = default;
        public int BaseHP = default;
        public bool HadMutations = false;
        public bool HadCybernetics = false;
        public bool IsAlive = false;
        public bool IsOrganic = false;

        [NonSerialized]
        public (string, string)[] CyberneticAndBodypart = new (string, string)[0];

        [NonSerialized]
        public (string, string)[] Properties = new (string, string)[0];

        [NonSerialized]
        public (string, int)[] IntProperties = new (string, int)[0];

        [NonSerialized]
        public (string, int)[] StatLevels = new (string, int)[0];

        [NonSerialized]
        public (string, int)[] MutationsWithLevels = new (string, int)[0]; //cap = mutations.count

        [NonSerialized]
        public (string, bool)[] BodyParts = new (string, bool)[0]; //i could just only store dismembered bodyparts and match them and dismember them instead of storing all bodyparts

        [NonSerialized]
        public string[] IParts = new string[0];

        [NonSerialized]
        public string[] Effects = new string[0];

        [NonSerialized]

        public string[] Skills = new string[0];

        public (string, object)[] Arrays => new (string, object)[]
        {
            (nameof(MutationsWithLevels), MutationsWithLevels),
            (nameof(IParts), IParts),
            (nameof(CyberneticAndBodypart), CyberneticAndBodypart),
            (nameof(Properties), Properties),
            (nameof(IntProperties), IntProperties),
            (nameof(StatLevels), StatLevels),
            (nameof(BodyParts), BodyParts),
            (nameof(Effects), Effects),
            (nameof(Skills), Skills)
        };

        public (string, object)[] SimpleFields => new (string, object)[]
        {
            (nameof(Blueprint), Blueprint),
            (nameof(DisplayName), DisplayName),
            (nameof(ID), ID),
            (nameof(HadMutations), HadMutations),
            (nameof(HadCybernetics), HadCybernetics),
            (nameof(Level), Level),
            (nameof(BaseHP), BaseHP),
            (nameof(IsAlive), IsAlive),
            (nameof(IsOrganic), IsOrganic)
        };

        public GameObjectDataRecord()
        {

        }

        public GameObjectDataRecord(GameObject Object)
        {
            Record(Object);
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Write(Writer, CyberneticAndBodypart);
            Write(Writer, Properties);
            Write(Writer, IntProperties);
            Write(Writer, StatLevels);
            Write(Writer, MutationsWithLevels);
            Write(Writer, BodyParts);
            Write(Writer, IParts);
            Write(Writer, Effects);
            Write(Writer, Skills);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            CyberneticAndBodypart = Read<(string, string)>(Reader);
            Properties = Read<(string, string)>(Reader);
            IntProperties = Read<(string, int)>(Reader);
            StatLevels = Read<(string, int)>(Reader);
            MutationsWithLevels = Read<(string, int)>(Reader);
            BodyParts = Read<(string, bool)>(Reader);
            IParts = Read<string>(Reader);
            Effects = Read<string>(Reader);
            Skills = Read<string>(Reader);
            base.Read(Basis, Reader);
        }

        static void Write<T>(SerializationWriter Writer, T[] array)
        {
            Writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Writer.WriteObject(array[i]);
        }

        static T[] Read<T>(SerializationReader Reader)
        {
            int count = Reader.ReadInt32();
            T[] array = new T[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = (T)Reader.ReadObject();
            }
            return array;
        }
        public void ReadData()
        {
            MetricsManager.LogInfo($"\nREADING INFO ON {DisplayName}, {Blueprint}, {ID} START");
            Read(SimpleFields);
            MetricsManager.LogInfo("\n ARRAYS STARTED");
            ReadArrays(Arrays);
            MetricsManager.LogInfo($"\nREADING INFO ON {DisplayName}, {Blueprint}, {ID} END");
        }
        void ReadArrays((string, object)[] arrays)
        {
            for (int i = 0; i < arrays.Length; i++)
            {
                MetricsManager.LogInfo($"\n{arrays[i].Item1} START");
                CastArray(arrays[i].Item2);
                MetricsManager.LogInfo($"\n{arrays[i].Item1} END");
            }
        }
        void Record(GameObject Object)
        {
            RecordMutations(Object);
            RecordIParts(Object);
            RecordSkills(Object);
            RecordStats(Object);
            RecordFX(Object);
            RecordStringProperties(Object);
            RecordIntProperties(Object);
            Blueprint = Object.Blueprint;
            Level = Object.Level;
            DisplayName = Object.DisplayName;
            BaseHP = Object.baseHitpoints;
            IsAlive = Object.IsAlive;
            IsOrganic = Object.IsOrganic;
            ID = Object.ID;
        }

        void RecordStats(GameObject Object)
        {
            if (Object.Statistics != null)
            {
                StatLevels = new (string, int)[Object.Statistics.Count];
                var stats = Object.Statistics.ToArray();
                for (int i = 0; i < stats.Length; i++)
                {
                    StatLevels[i].Item1 = stats[i].Key;
                    StatLevels[i].Item2 = stats[i].Value.Value;
                }
            }
        }
        void RecordSkills(GameObject Object)
        {
            var skills = Object.GetPart<Skills>();
            if (skills != null)
            {
                Skills = new string[skills.SkillList.Count];
                for (int i = 0; i < skills.SkillList.Count; i++)
                {
                    Skills[i] = skills.SkillList[i].Name;
                }
            }

        }

        void RecordIParts(GameObject Object)
        {
            IParts = new string[GetSize(Object)];
            int index = 0;
            for (int i = 0; i < Object.PartsList.Count; i++)
            {
                if (CheckType(Object.PartsList[i]))
                {
                    IParts[index] = Object.PartsList[i].Name;
                    index++;
                }
                if (index >= IParts.Length)
                    break;
            }
        }
        void RecordIntProperties(GameObject Object)
        {
            IntProperties = new (string, int)[Object.IntProperty.Count];
            var array = Object.IntProperty.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                IntProperties[i].Item1 = array[i].Key;
                IntProperties[i].Item2 = array[i].Value;
            }

        }

        void RecordStringProperties(GameObject Object)
        {
            Properties = new (string, string)[Object.Property.Count];
            var array = Object.Property.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                Properties[i].Item1 = array[i].Key;
                Properties[i].Item2 = array[i].Value;
            }
        }


        void RecordFX(GameObject Object)
        {
            Effects = new string[Object.Effects.Count];
            for (int i = 0; i < Object.Effects.Count; i++)
                Effects[i] = Object.Effects[i].ClassName;
        }

        void RecordMutations(GameObject Object)
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

        static void CastArray(object obj)
        {
            switch (obj)
            {
                case (string, int)[] stringIntArray:
                    Read(stringIntArray);
                    break;
                case (string, bool)[] stringBoolArray:
                    Read(stringBoolArray);
                    break;
                case (string, string)[] stringStringArray:
                    Read(stringStringArray);
                    break;
                case string[] stringArray:
                    Read(stringArray);
                    break;
            }
        }


        static int GetSize(GameObject Object)
        {
            int size = 0;
            for (int i = 0; i < Object.PartsList.Count; i++)
            {
                if (CheckType(Object.PartsList[i]))
                    size++;
            }
            return size;
        }

        static bool CheckType(IPart part) => part is not BaseMutation or BaseSkill;
    }
}