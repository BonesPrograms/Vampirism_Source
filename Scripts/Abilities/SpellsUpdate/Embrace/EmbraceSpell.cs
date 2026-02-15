using System;
using Nexus.Rules;
using Nexus.Properties;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using System.Collections.Generic;
using XRL.Wish;
using System.Collections;


namespace XRL.World.Parts
{
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
        //Budding
        //one important thing: make sure the corpse is not blueprint ashes, lol. and organic and other stuff too i suppose
        ///could maybe run scan applicable on the ID
        //this will have listener for companion limit
        //i can draw from beguiling to see how to add new chat options like "follow"

        void Embrace()
        {
            Cell cell = ParentObject.PickDirection(EMBRACE.ABILITY_NAME);
            if (cell != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var Object = cell.Objects[i];
                    if (Object.TryGetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, out string result))
                    {
                        FinalizeEmbrace(Object, result);
                        return; //bug here would list EVERY object in the cell. we just take the first object with the flag. i dont really care if corpses are stacked, the player can deal with that
                    }   //(because the game already has issues with trying to easily/quickly target two objets occupying the same cell)
                }
            }
        }


        //Embraced.t() rises from the dead!

        //could be buggy but i dont really care if you have to destroy a corpse to access the corpse underneath it
        //else      //reduces a lot of work on my end if i just get the first possible object and return
        //  SimulateParentObject(Object);
        //gets the first corpse with the embraceable property in a cell
        void FinalizeEmbrace(GameObject Object, string result)
        {
            if (Object.HasEffect<Embraced>())
            {
                UI.Popup.Show($"{Object.t()} is already being Embraced.");
            }
            else if (result == FLAGS.TRUE)
            {
                if (Object.GetIntProperty(FLAGS.EMBRACE.LEVEL_ON_DEATH) > Level + ParentObject.Level)
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

        }

        void Cast(GameObject Object)
        {
            if (base.Cast("to embrace"))
            {
                base.ExpendBlood(false, $"You pour your blood down {Object.t()}'s throat.");
                if (RealityCheck(Object.CurrentCell))
                    Vampirize(Object);
            }
        }

        void Vampirize(GameObject Object) //fun secret : this was debug code that has now become content
        {
            string blueprint = Object.GetStringProperty("SourceBlueprint");
            string id = Object.GetStringProperty("SourceID");
            int level = Object.GetIntProperty(FLAGS.EMBRACE.LEVEL_ON_DEATH);
            GameObject person = GameObject.Create(blueprint);
            Object.CurrentCell.AddObject(person);
            person.Statistics["Level"]._Value = level;
            Mutations m = person.RequirePart<Mutations>();
            m.AddMutation(nameof(Vampirism));
            //person.ApplyEffect(new Embraced());
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Nexus.Rules.EMBRACE.COMMAND_NAME)
            {
                Embrace();
            }
            return base.HandleEvent(E);
        }
    }

    [Serializable]

    public class GameObjectCopy : IPart
    {
        public string DisplayName = default;
        public bool HadMutations = default;
        public bool HadCybernetics = default;
        public int Level = default;
        public string Blueprint = default;
        public (string, string)[] CyberneticAndBodypart = default;
        public (string, string)[] StringProperties = default;
        public (string, long)[] LongProperties = default;
        public (string, int)[] IntProperties = default;
        public (string, int)[] StatLevels = default;
        public (string, int)[] MutationsWithLevels = default; //cap = mutations.count
        public (string, bool)[] BodyParts = default; //bool false = dismembered
        public string[] IParts = default;
        //NEW ARRAY IDEA:
        //should we tag relations as well? opinions of the player? yes a list of Opinions to the player at least would be valid

        //OTHER ARRAY IDEA:
        //SKILLS string
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

        //plan: we will requirepart by string name
        //we prob wont create an instance from a blueprint, well create an instance of their blueprint to match things like physics and  render and displayname stuff maybe idk. lots of parts to add lowkey.

        // final note: in scan, have it show object level specifically. additionally will need to see mutations w/ levels and skills and cybernetics etc for our copier. 
        // thank god there is a mutation lsit i can past that to one of my loggers. will need otherstuff like bodyparts too - anything listed in Copy. new wish "CheckCopy"

        public (string, object)[] SimpleFields => new (string, object)[]
        {
            (nameof(HadMutations), HadMutations),
            (nameof(HadCybernetics), HadCybernetics),
            (nameof(Level), Level),
            (nameof(Blueprint), Blueprint),
            (nameof(DisplayName), DisplayName)
        };

        public GameObjectCopy(GameObject Object)
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

        // GameObject Recreate()
        // {
        //     GameObject Object = GameObject.Create(Blueprint);

        // }
    }

}
