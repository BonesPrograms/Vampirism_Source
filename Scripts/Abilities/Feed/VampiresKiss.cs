using System;
using XRL.Core;
using VampirismSys.Extensions;
using VampirismSys.Rules;
using XRL.World.Parts;
using VampirismSys.Core;

namespace XRL.World.Effects
{
    /// <summary>
    /// The Exhausted-based stunning effect that incapacitates victims of Feeding.
    /// </summary>
    [Serializable]
    public class VampiresKiss : Exhausted
    {
        GameObject Feeder;
        public VampiresKiss() 
        {
            Duration = Feed.DURATION;
        }

        internal VampiresKiss(GameObject feeder) : this()
        {
            Feeder = feeder;
        }


        public override string GetDescription() => "{{R sequence|vampire's kiss}}";
        public override string GetStateDescription() => "{{R sequence|vampire's kiss}}";
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (Feeder?.HasEffectDescendedFrom<BaseFeedEffect>() ?? false)
            {
                base.Object.ParticleText("{{K|*remains stunned*}}");
                base.Object.PassTurn();
            }
            else
            {
                Duration = 0;
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

        public override void Remove(GameObject Object)
        {
            if (!base.Object.MakeSave("Toughness", 13, null, null, "Dazed From Kiss") && !Object.Unaware(true))
                base.Object.ApplyEffect(new Dazed(WikiRng.Next(16, 20)));

        }
        public override bool SameAs(Effect e) => false;
        public override bool Render(RenderEvent E) => true;

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.WriteGameObject(Feeder);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Feeder = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }
    }
}