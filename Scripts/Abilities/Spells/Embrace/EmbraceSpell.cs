using System;
using VampirismSys.Properties;
using XRL.World.Effects;
using System.Linq;
using VampirismSys.Extensions;
using XRL.Messages;
using VampirismSys.Core;

namespace XRL.World.Parts
{

    [Serializable]
    public class EmbraceableObject : IPart
    {
        public GameObject Object { get => _object; private init { _object = value; } }
        GameObject _object;
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.WriteGameObject(_object);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            _object = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }
        public EmbraceableObject()
        {

        }
        public EmbraceableObject(GameObject Object)
        {
           this.Object = Object.DeepCopy();
        }
    }

    [Serializable]
    public class EmbraceSpell : BaseVampireSpell
    {
        protected override int Cooldown => VampirismSys.Rules.Embrace.COOLDOWN;

        bool Roll(GameObject Object)
        {
            return Object.GetIntProperty(Flags.Embrace.LEVEL_ON_DEATH) < Level + ParentObject.Level;
        }
        public EmbraceSpell()
        {
            CommandName = VampirismSys.Rules.Embrace.COMMAND_NAME;
            AbilityMenuName = VampirismSys.Rules.Embrace.ABILITY_NAME;
        }
        protected override void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("Attack", Level + ParentObject.Level);
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Cooldown);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == VampirismSys.Rules.Embrace.COMMAND_NAME && VampirismSys.Extensions.Checks.Prerequisites(base.ParentObject, VampirismSys.Rules.Embrace.ABILITY_NAME, "embrace"))
            {
                FindEmbraceableObject();
            }
            return base.HandleEvent(E);
        }
        void FindEmbraceableObject()
        {
            Cell cell = base.ParentObject.PickDirection(VampirismSys.Rules.Embrace.ABILITY_NAME);
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
                if (Roll(Object))
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
            obj.ApplyEffect(new KnockedOut(time, true));
            obj.ApplyEffect(new BeingEmbracedFX(time, Level));
            obj.hitpoints = 2;
            Object.Obliterate();
            MessageQueue.Suppress = false;
        }


    }

}




