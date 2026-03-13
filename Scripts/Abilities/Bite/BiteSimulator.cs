using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using VampirismSys.Core;
using XRL.World;
using System.Linq;
using HarmonyLib;

namespace VampirismSys.Biting
{
    /// <summary>
    /// Handles the simulation features for what happens when biting targets with various toxic or otherwise inedible conditions.
    /// </summary>

    internal class BiteSimulator : BaseBite
    {
         readonly Bite _source;
         readonly LiquidBehaviors _liquidBehaviors;
        internal BiteSimulator(GameObject Biter, Bite Source) : base(Biter)
        {
            this._source = Source;
            _liquidBehaviors = new(Biter);
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
            if (_source.Diseases[0].Item2 || _source.Diseases[1].Item2)
                Glotrot();
            else if (_source.Diseases[2].Item2 || _source.Diseases[3].Item2)
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

        internal Ending BadEnding(GameObject Target)
        {
            return Result(_source.Flags.Where(x => x.Item2).Select(x => Cycle(x.Item1, Target)));
        }

        Ending Cycle(string flag, GameObject Target) =>
        flag switch
        {
            nameof(_source.IsOnFire) => FlameEnding(Target),
            nameof(_source.HasPlasma) => PlasmaEnding(),
            nameof(_source.HasBadLiquid) => _liquidBehaviors.LiquidEnding(_source.BadLiquids),
            nameof(_source.HasDisease) => DiseaseEnding(),
            nameof(_source.IsPoisoned) => PoisonEnding(),
            _ => default
        };

    }
}
