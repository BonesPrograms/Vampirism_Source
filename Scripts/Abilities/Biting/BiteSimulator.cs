using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using Nexus.Core;
using XRL.World;

namespace Nexus.Biting
{
    /// <summary>
    /// Handles the simulation features for what happens when biting targets with various toxic or otherwise inedible conditions.
    /// </summary>

    class BiteSimulator : VampireBite
    {
        readonly public Bite Source;
        public BiteSimulator(GameObject Biter, GameObject Target, Bite Source) : base(Biter, Target) => this.Source = Source;
        Ending FlameEnding()
        {
            Biter.TemperatureChange(+Target.Temperature);
            if (PainTolerance())
                return Ending.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{R sequence|IT BURNS!}}");
            return MakeSave("Bit Flaming Target");
        }
        Ending PlasmaEnding()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's {{plasma|plasma}}! Ouch!");
            Biter.TakeDamage(WikiRng.Next(5, 10), "Plasma", null, null);
            Biter.ApplyEffect(new CoatedInPlasma(WikiRng.Next(10, 15), Biter));
            return MakeSave("Bit Plasma Coated Target");
        }

        Ending PoisonEnding()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's {{G sequence|poisonous!}} You feel sick!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(6, 9), $"{WikiRng.Next(-1, 5)}", 1, Biter)); // will this be buggy?
            return MakeSave("Drank Poisonous Blood");

        }
        Ending DiseaseEnding() //this is impossible to succeed on, it is the worst one
        {
            if (Source.Diseases[typeof(Glotrot)] || Source.Diseases[typeof(GlotrotOnset)])
                Glotrot();
            else if (Source.Diseases[typeof(Ironshank)] || Source.Diseases[typeof(IronshankOnset)])
                Ironshank();
            return Ending.VOMIT;

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



        public Ending BadEnding()
        {
            List<Ending> endresults = new();
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
            return Result(endresults);
        }
    }
}
