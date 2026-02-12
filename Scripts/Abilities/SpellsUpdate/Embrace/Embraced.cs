using System;
using XRL.World.Parts;
using Nexus.Rules;
using Nexus.Core;

namespace XRL.World.Effects

{
    [Serializable]
    public class Embracing : Effect
    {

    }

    [Serializable]
    public class Embraced : Effect
    {
        public Embraced()
        {
            Duration = 9999;
            DisplayName = "";
        }

        //furthermore
        //you embrace people at your vampirism level (maybe)
        //doesnt rly make sense
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
            if (E.Effect is IFeeding feed && feed.isAttacker && feed.Object == Object)
                Duration = 0;
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (!Beast.frenzied && !Beast.Incap() && Beast.HasFangs())        
                Beast.Core.EmbraceFrenzy();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Obj)
        {
            if (!Obj?.IsPlayer() ?? false)
            {
                Vitae v = Obj.GetPart<Vitae>();
                v.SetBlood(VITAE.BLOOD_QUENCHED);
            }
        }

        public override bool Apply(GameObject Obj)
        {
            Vitae v = Obj?.GetPart<Vitae>();
            v?.SetBlood(VITAE.BLOOD_MIN);
            return true;
        }

    }
}