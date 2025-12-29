using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using System.Linq;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using XRL.World;

namespace Nexus.Bite
{
    /// <summary>
    /// Handles the simulation features for what happens when biting targets with various toxic or otherwise inedible conditions.
    /// </summary>

    class BiteSimulator : VampireBite
    {
        readonly public Biting Source;
        public BiteSimulator(GameObject Biter, Biting Source) : base(Biter) => this.Source = Source;
        Endings FlameEnding()
        {
            Biter.TemperatureChange(+Source.Target.Temperature);
            if (PainTolerance())
                return Endings.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{R sequence|IT BURNS!}}");
            return MakeSave("Bit Flaming Target");
        }
        Endings PlasmaEnding()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's {{plasma|plasma}}! Ouch!");
            Biter.TakeDamage(WikiRng.Next(5, 10), "Plasma", null, null);
            Biter.ApplyEffect(new CoatedInPlasma(WikiRng.Next(10, 15), Source.Biter));
            return MakeSave("Bit Plasma Coated Target");
        }

        Endings PoisonEnding()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's {{G sequence|poisonous!}} You feel sick!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(6, 9), $"{WikiRng.Next(-1, 5)}", 1, Source.Biter)); // will this be buggy?
            return MakeSave("Drank Poisonous Blood");

        }
        Endings DiseaseEnding() //this is impossible to succeed on, it is the worst one
        {
            if (Source.Diseases[typeof(Glotrot)] || Source.Diseases[typeof(GlotrotOnset)])
                Glotrot();
            else if (Source.Diseases[typeof(Ironshank)] || Source.Diseases[typeof(IronshankOnset)])
                Ironshank();
            return Endings.VOMIT;

        }

        void Ironshank()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It {{ironshank|stiff!}} You feel sick!");
            Biter.ApplyEffect(new IronshankOnset());
        }

        void Glotrot()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's {{k sequence|rotten!}} You feel sick!");
            Biter.ApplyEffect(new GlotrotOnset());
        }



        public Endings BadEnding()
        {
            List<Endings> endresults = new();
            if (Source.IsOnFire)
                endresults.Add(FlameEnding());
            if (Source.HasBadLiquid)
                endresults.Add(new LiquidBehaviors(Biter, Source.BadLiquids).LiquidEnding());
            if (Source.HasPlasma)
                endresults.Add(PlasmaEnding());
            if (Source.IsPoisoned)
                endresults.Add(PoisonEnding());
            if (Source.HasDisease)
                endresults.Add(DiseaseEnding());
            return endresults.Count != 0 ? endresults.Min() : Endings.OUT_OF_RANGE;
        }
    }
}
