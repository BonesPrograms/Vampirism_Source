using System;
using Nexus.Rules;
using Nexus.Properties;
using XRL.World.Effects;
using System.Linq;

using Nexus.Core;
using XRL.Messages;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{

    [Serializable]
    public class EmbraceableObject : IPart
    {
        [NonSerialized]
        public GameObject Object;
        public EmbraceableObject()
        {

        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.WriteGameObject(Object);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Object = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }
        public EmbraceableObject(GameObject Object)
        {
            this.Object = Object.DeepCopy();
        }
    }

    [Serializable]
    public class EmbraceSpell : VampiricSpell
    {

        public override string CommandName => Nexus.Rules.Embrace.COMMAND_NAME;
        public override string AbilityMenuName => Nexus.Rules.Embrace.ABILITY_NAME;
        public override int Cooldown => Nexus.Rules.Embrace.COOLDOWN;
        public override void CollectStats(Templates.StatCollector stats)
        {
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == PooledEvent<GetCompanionLimitEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(GetCompanionLimitEvent E)
        {
            if (E.Means == "Sire" && E.Actor == ParentObject && SpellID != Guid.Empty)
            {
                E.Limit= E.Limit + 2;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Nexus.Rules.Embrace.COMMAND_NAME && Nexus.Core.Checks.Prerequisites(base.ParentObject, Nexus.Rules.Embrace.ABILITY_NAME, "embrace"))
            {
                FindEmbraceableObject();
            }
            return base.HandleEvent(E);
        }
        void FindEmbraceableObject()
        {
            Cell cell = base.ParentObject.PickDirection(Nexus.Rules.Embrace.ABILITY_NAME);
            bool? value = cell?.Objects?.Any(x => { if (x.TryGetStringProperty(Flags.Embrace.EMBRACEABLE, out string result)) { CheckEmbraceableObject(x, result); return true; } return false; });
            if (value == false)
                UI.Popup.Show("There is nothing there to embrace.");
        }


        void CheckEmbraceableObject(GameObject Object, string result)
        {
            if (Object.HasEffect<AfterEmbracedFX>())
            {
                UI.Popup.Show($"{Object.t()} is already being embraced.");
            }
            else if (result == Flags.TRUE)
            {
                if (Object.GetIntProperty(Flags.Embrace.LEVEL_ON_DEATH) < Level + ParentObject.Level)
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
                UI.Popup.Show($"You cannot embrace {Object.t()}.");
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
            MessageQueue.Suppress = true;
            var copy = Object.GetPart<EmbraceableObject>();
            GameObject obj = copy.Object;
            obj.MakeActive();
            Object.CurrentCell.AddObject(obj);
            int time = WikiRng.Next(50, 100);
            obj.ApplyEffect(new Asleep(time, true, false, false, true));
            obj.ApplyEffect(new BeingEmbracedFX(ParentObject, time, Level));
            obj.hitpoints = 2;
            Object.Obliterate();
            MessageQueue.Suppress = false;
        }


    }

}




