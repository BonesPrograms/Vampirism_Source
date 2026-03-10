using System;
using XRL.World.Parts;
using Nexus.Rules;
using Nexus.Core;
using Nexus.Spells;
using XRL.World.Parts.Mutation;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects

{
    [Serializable]
    public class BeingEmbracedFX : IScribedEffect
    {
        public GameObjectReference Embracer;
        public bool FailedEmbrace;
        public int Level;
        public BeingEmbracedFX()
        {
        }
        public BeingEmbracedFX(GameObject Embracer, int time, int level)
        {
            this.Embracer = Embracer.Reference();
            base.Duration = time;
            this.Level = level;
        }

        public override string GetDescription()
        {
            return "{{r|embracing}}";
        }

        public override bool UseStandardDurationCountdown()
        {
            return true;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID || ID == BeforeApplyDamageEvent.ID || ID == DeathEvent.ID || ID == BeforeDieEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (E.Dying == Object && !FailedEmbrace)
            {
                Object.hitpoints = 1;
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(DeathEvent E)
        {
            if (E.Dying == Object)
            {
                BurnToAshes(E.Dying);
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeApplyDamageEvent E)
        {
            if (E.Object == Object)
            {
                if (E.Damage.Attributes.Contains("Fire"))
                {
                    Message($"Fire disrupts the embracing of {Object.t()}!");
                    FailedEmbrace = true;
                    Duration = 0;
                }
                else
                {
                    NotifyTargetImmuneEvent.Send(E.Weapon, E.Object, E.Actor, E.Damage, this);
                    E.Damage.Amount = 0;
                    return false;
                }
            }
            return base.HandleEvent(E);
        }

        void Message(string text)
        {
            if (The.Player.HasLOSTo(Object))
                AddPlayerMessage(text);
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            if (Vampirism.SunlightInterference(Object))
            {
                Message($"Sunlight disrupts the embracing of {Object.t()}!");
                FailedEmbrace = true;
                Duration = 0;
            }
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Object)
        {
            if (!FailedEmbrace)
            {
                Object.RestorePristineHealth(); //not sure if i want them to regenerate limbs, but this is our current fix for Heal not working (they would die after waking up)
                int paleHP = Object.baseHitpoints / 4; //pale comes at 50% hp so we make it a little lower so it lasts a bit, but not too low! either way they wont be a challenge to kill with such low HP
                Object.hitpoints = paleHP;
                Object.RequireMutation<Vampirism>(Level);
                Object.SetStringProperty(Nexus.Properties.Flags.FLEDGLING, null);
                Object.ApplyEffect(new AfterEmbracedFX());
                Object.ApplyEffect(new Pale(999));
                Message($"{Object.t()} rises from the dead!");
            }
            else
                BurnToAshes(Object);
        }

        void BurnToAshes(GameObject Object)
        {
            Object.CurrentCell?.AddObject(GameObject.Create("Ashes"));
            Message($"{Object.t()} burns to ashes!");
            Object.Obliterate();
        }

    }
    [Serializable]
    public class AfterEmbracedFX : IScribedEffect
    {
        public AfterEmbracedFX()
        {
            Duration = 9999;
            DisplayName = "";
        }
        public override string GetDescription() => "{{r|embraced}}";
        public sealed override string GetDetails() => "A newly embraced flegling vampire that has yet to feed.";
        bool Roll => WikiRng.Next(1, 100) == 100; //ridiculously high frenzy chance. not always instant, enough time for you to back up a bit or something
        TheBeast _Beast;
        public TheBeast Beast => _Beast ??= Object.GetPart<TheBeast>();
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID)
                return true;
            if (Roll && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return !Beast.CantFrenzy();
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            if (E.Effect is IFeeding feed && feed.isAttacker)
                Duration = 0;
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            Beast.Core.Frenzy();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Obj)
        {
            if (!Obj.IsPlayer())
            {
                Parts.Vitae v = Obj.GetPart<Parts.Vitae>();
                v.Blood = Nexus.Rules.Vitae.BLOOD_QUENCHED;
            }
        }

        public override bool Apply(GameObject Obj)
        {
            Parts.Vitae v = Obj.GetPart<Parts.Vitae>();
            v.Blood = Nexus.Rules.Vitae.BLOOD_MIN;
            return true;
        }

    }
}