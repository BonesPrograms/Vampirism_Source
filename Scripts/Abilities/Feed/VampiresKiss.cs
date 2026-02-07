using System;
using XRL.Core;
using Nexus.Core;

namespace XRL.World.Effects
{
    /// <summary>
    /// The Exhausted-based stunning effect that incapacitates victims of Feeding.
    /// </summary>
    [Serializable]
    public class Vampires_Kiss : Exhausted
    {
        public Vampires_Kiss() => DisplayName = "vampire's kiss";
        public Vampires_Kiss(int Duration) : this() => base.Duration = Duration;
        public override string GetDescription() => "{{R sequence|vampire's kiss}}";
        public override string GetStateDescription() => "{{R sequence|vampire's kiss}}";
        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == PooledEvent<IsConversationallyResponsiveEvent>.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (E.Object == base.Object)
            {
                if (base.Object.IsPlayer())
                    XRLCore.Core.RenderDelay(500);
                else
                    base.Object.ParticleText("{{K|*remains stunned*}}");
                base.Object.ForfeitTurn();
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(IsConversationallyResponsiveEvent E)
        {
            if (E.Speaker == base.Object)
            {
                if (E.Mental && !E.Physical)
                {
                    E.Message = base.Object.Poss("mind") + " is in disarray.";
                }
                else
                {
                    E.Message = base.Object.Does("can't") + " respond to you.";
                }

                return false;
            }

            return base.HandleEvent(E);
        }

        public override bool Apply(GameObject Object)
        {
            if (Object.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You are {{K|stunned}}!");
            Object.ParticleText("*stunned*", 'K');
            Object.ForfeitTurn();
            return true;
        }

        public void Remove()
        {
            if (!base.Object.MakeSave("Toughness", 13, null, null, "Dazed From Kiss") && !Object.Unaware(true))
                base.Object.ApplyEffect(new Dazed(WikiRng.Next(16, 20)));

        }
        public override bool SameAs(Effect e) => false;
        public override bool Render(RenderEvent E) => true;
    }
}