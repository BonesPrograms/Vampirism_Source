using XRL.UI;
using XRL.World.Effects;
using VampirismSys.Extensions;
using XRL.World;
using System.Linq;
using VampirismSys.Core;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Biting
{
    /// <summary>
    /// Handles the simulation features for what happens when biting targets with various toxic or otherwise inedible conditions.
    /// </summary>

    public class BiteSimulator : BaseBite
    {
        readonly Bite _bite;
        readonly LiquidBehaviors _liquidBehaviors;
        public BiteSimulator(Bite bite, Vampirism source) : base(source)
        {
            this._bite = bite;
            _liquidBehaviors = new(source);
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
            if (_bite.Diseases[0].Item2 || _bite.Diseases[1].Item2)
                Glotrot();
            else if (_bite.Diseases[2].Item2 || _bite.Diseases[3].Item2)
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

        public Ending BadEnding(GameObject Target)
        {
            return Result(_bite.Flags.Where(x => x.Item2).Select(x => Cycle(x.Item1, Target)));
        }

        Ending Cycle(string flag, GameObject Target) =>
        flag switch
        {
            nameof(_bite.IsOnFire) => FlameEnding(Target),
            nameof(_bite.HasPlasma) => PlasmaEnding(),
            nameof(_bite.HasBadLiquid) => _liquidBehaviors.LiquidEnding(_bite.BadLiquids),
            nameof(_bite.HasDisease) => DiseaseEnding(),
            nameof(_bite.IsPoisoned) => PoisonEnding(),
            _ => default
        };

    }
}
