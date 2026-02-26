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
    public class Embracing : IScribedEffect
    {
        public GameObjectReference Embracer;
        public bool FailedEmbrace;
        public int Level;

        public Embracing()
        {

        }
        public Embracing(GameObject Embracer, int time, int level)
        {
            this.Embracer = Embracer.Reference();
            base.Duration = time;
            this.Level = level;
        }

        public override bool UseStandardDurationCountdown()
        {
            return true;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == SingletonEvent<EndTurnEvent>.ID || ID == BeforeTookDamageEvent.ID || ID == DeathEvent.ID || ID == BeforeDieEvent.ID)
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

        public override bool HandleEvent(BeforeTookDamageEvent E)
        {
            if (E.Object == Object && E.Damage.Attributes.Contains("Fire") && UI.Options.GetOptionBool(OPTIONS.FIRE))
            {
                Message($"Fire disrupts the embracing of {Object.t()}!");
                FailedEmbrace = true;
                Duration = 0;
            }
            if (E.Object == Object && !E.Damage.Attributes.Contains("Fire"))
            {
                E.Damage.Amount = 0;
                return false;
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
            if (SpellCore.SunlightInterference(Object))
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
                Object.RequireMutation<Vampirism>(Level);
                Fledgling part = new(Embracer?.Object, false);
                Object.AddPart(part);
                Object.ApplyEffect(new Embraced());
                Object.ApplyEffect(new Pale(999));
                Object.Heal(Object.baseHitpoints / 2);
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
    public class Embraced : IScribedEffect
    {
        public Embraced()
        {
            Duration = 9999;
            DisplayName = "";
        }
        public override string GetDescription() => "{{r|embraced}}";
        public sealed override string GetDetails() => "A newly embraced flegling vampire that has yet to feed.";
        bool Roll => WikiRng.Next(1, 100) == 100; //ridiculously high frenzy chance
        TheBeast _Beast;
        public TheBeast Beast => _Beast ??= Object.GetPart<TheBeast>();
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID)
                return true;
            if (Roll && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
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
            if (!Beast.CantFrenzy())
                Beast.Core.Frenzy();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Obj)
        {
            if (!Obj.IsPlayer())
            {
                Vitae v = Obj.GetPart<Vitae>();
                v.SetBlood(VITAE.BLOOD_QUENCHED);
            }
        }

        public override bool Apply(GameObject Obj)
        {
            Vitae v = Obj.GetPart<Vitae>();
            v.SetBlood(VITAE.BLOOD_MIN);
            return true;
        }

    }
}