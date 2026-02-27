using System;
using System.Linq;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using System.Collections.Generic;
using Nexus.Core;

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
            Writer.WritePrimitiveArray(CyberneticAndBodypart);
            Writer.WritePrimitiveArray(Properties);
            Writer.WritePrimitiveArray(IntProperties);
            Writer.WritePrimitiveArray(StatLevels);
            Writer.WritePrimitiveArray(MutationsWithLevels);
            Writer.WritePrimitiveArray(BodyParts);
            Writer.WritePrimitiveArray(IParts);
            Writer.WritePrimitiveArray(Effects);
            Writer.WritePrimitiveArray(Skills);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            CyberneticAndBodypart = Reader.ReadPrimitiveArray<string, string>();
            Properties = Reader.ReadPrimitiveArray<string, string>();
            IntProperties = Reader.ReadPrimitiveArray<string, int>();
            StatLevels = Reader.ReadPrimitiveArray<string, int>();
            MutationsWithLevels = Reader.ReadPrimitiveArray<string, int>();
            BodyParts = Reader.ReadPrimitiveArray<string, bool>();
            IParts = Reader.ReadPrimitiveArray<string>();
            Effects = Reader.ReadPrimitiveArray<string>();
            Skills = Reader.ReadPrimitiveArray<string>();
            base.Read(Basis, Reader);
        }
        static void CleanConsole()
        {
            for (int i = 0; i < 25; i++)
                MetricsManager.LogInfo("\n");
        }
        public void ReadData()
        {
            CleanConsole();
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
            RecordIParts(Object.PartsList);
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
        void RecordIntProperties(GameObject Object)
        {
            IntProperties = Object.IntProperty.TupleArray();
        }

        void RecordStringProperties(GameObject Object)
        {
            Properties = Object.Property.TupleArray();
        }

        void RecordFX(GameObject Object)
        {
            Effects = GetTypeNames(Object.Effects);
        }

        void RecordSkills(GameObject Object)
        {
            var skills = Object.GetPart<Skills>();
            if (skills != null)
                Skills = GetTypeNames(skills.SkillList);
        }

        void RecordMutations(GameObject Object)
        {
            Mutations m = Object.GetPart<Mutations>();
            if (m != null && m.MutationList.Count > 0)
            {
                MutationsWithLevels = new (string, int)[m.MutationList.Count];
                MutationsWithLevels.AssignEachIndexed(delegate (int i) { (string, int) tuple = new() { Item1 = m.MutationList[i].Name, Item2 = m.MutationList[i].Level }; return tuple; });
                HadMutations = true;

            }
        }

        void RecordStats(GameObject Object)
        {
            if (Object.Statistics != null)
            {
                StatLevels = new (string, int)[Object.Statistics.Count];
                int index = 0;
                Object.Statistics.ForEach(delegate (KeyValuePair<string, Statistic> obj) { StatLevels[index].Item1 = obj.Key; StatLevels[index].Item2 = obj.Value.Value; index++; });
            }
        }

        void RecordIParts(PartRack parts)
        {
            IParts = new string[GetSize(parts)];
            int index = 0;
            parts.IfEachCount(ref index, IParts.Length, delegate (IPart obj)
            {
                if (!CheckType(obj))
                {
                    IParts[index] = obj.Name;
                    return true;
                }
                return false;
            });
        }

        static string[] GetTypeNames<T>(IList<T> list)
        {
            string[] array = new string[list.Count];
            array.AssignEachIndexed(delegate (int i) { return list[i].GetType().Name; });
            return array;
        }

        static void Read<T>((string, T)[] array)
        {
            array.ForEach(x => MetricsManager.LogInfo($"{x.Item1}. {x.Item2}"));
        }
        static void Read(string[] array)
        {
            array.ForEach(x => MetricsManager.LogInfo(x));
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

        static int GetSize(PartRack rack)
        {
            return rack.ObjectCount(delegate (IPart part) { return !CheckType(part); });
        }
        static bool CheckType(IPart part) => part is BaseMutation or BaseSkill;
    }
}