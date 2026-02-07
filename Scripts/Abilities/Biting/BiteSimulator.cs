using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using Nexus.Core;
using XRL.World;
using System.Linq;
using HarmonyLib;

namespace Nexus.Biting
{
    /// <summary>
    /// Handles the simulation features for what happens when biting targets with various toxic or otherwise inedible conditions.
    /// </summary>

    public class BiteSimulator : VampireBite
    {
        readonly public Bite Source;
        readonly public LiquidBehaviors LiquidBehaviors;
        public BiteSimulator(GameObject Biter, Bite Source) : base(Biter)
        {
            this.Source = Source;
            LiquidBehaviors = new(Biter);
        }
        Ending FlameEnding(GameObject Target)
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
            if (Source.Diseases[0].Item2 == true || Source.Diseases[1].Item2 == true)
                Glotrot();
            else if (Source.Diseases[2].Item2 == true || Source.Diseases[3].Item2 == true)
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
        int Capacity()
        {
            int value = 0;
            for (int i = 0; i < Source.Flags.Length; i++)
            {
                if (Source.Flags[i] == true)
                    value++;
            }
            return value;
        }

        public Ending BadEnding(GameObject Target)
        {
            Ending[] endings = new Ending[Capacity()];
            int index = 0;
            if (Source.IsOnFire)
            {
                endings[index] = FlameEnding(Target);
                index++;
            }
            if (Source.HasBadLiquid)
            {
                endings[index] = LiquidBehaviors.LiquidEnding(Source.BadLiquids);
                index++;
            }
            if (Source.HasPlasma)
            {
                endings[index] = PlasmaEnding();
                index++;
            }
            if (Source.IsPoisoned)
            {
                endings[index] = PoisonEnding();
                index++;
            }
            if (Source.HasDisease)
                endings[index] = DiseaseEnding();

            return Result(endings);
        }
    }
}
