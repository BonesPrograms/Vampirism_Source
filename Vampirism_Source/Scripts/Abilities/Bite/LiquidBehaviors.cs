using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using System.Linq;
using Nexus.Core;
using XRL.World;

namespace Nexus.Bite
{

    class LiquidBehaviors : VampireBite
    {
        readonly Dictionary<string, bool> BadLiquids;
        public LiquidBehaviors(GameObject Biter, Dictionary<string, bool> BadLiquids) : base(Biter) => this.BadLiquids = BadLiquids;
        Endings OilLiquid() //yeah drinking oil doesnt really do anything but its gross so i added it. used to make you puke but it doesnt do that ingame so i got rid of that
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("{{K sequence|Disgusting!}}.");
            return Endings.SUCCESS;
        }
        Endings AcidLiquid()
        {
            Biter.TakeDamage(WikiRng.Next(5, 10), "Acid", null, null);
            if (PainTolerance())
                return Endings.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{G sequence|IT BURNS!}}");
            return MakeSave("Bit Acidic Target");

        }

        Endings AsphaltLiquid()
        {
            Biter.TakeDamage(WikiRng.Next(5, 10), "Asphalt", null, null);
            Biter.TemperatureChange(+200);
            if (PainTolerance())
                return Endings.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{K sequence|It burns!}}");
            return MakeSave("Bit Flaming Target");
        }

        Endings GrossLiquid() //this ones kind of funny it just makes you puke. automatic failure
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's disgusting!");
            return Endings.VOMIT;
        }

        Endings SlimeLiquid() //favorite one because youre most likely to encounter slime covered enemies compared to any of these other liquids
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("Its disgustingly slimy!");
            Biter.ApplyEffect(new Confused(WikiRng.Next(6, 12), 5, 7)); //and its probably one of the worst effects on the list aside from disease
            return Endings.FAIL;
        }

        Endings SludgeLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's horrifying! You feel sick!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(8, 12), $"{WikiRng.Next(4, 6)}", 10));
            return Endings.VOMIT;
        }

        Endings GooLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("its awful! You feel poison coursing through your veins!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(8, 12), $"{WikiRng.Next(4, 6)}", 10));
            return Endings.VOMIT;

        }

        Endings OozeLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's repulsive! You feel sick!");
            if (WikiRng.Next(1, 2) == 1) //funny thing about this during testing i found that even if i directly apply glotrot w/o rng, it may apply ironshank. wierd behavior, not sure if it was my fault, or
                Biter.ApplyEffect(new GlotrotOnset()); //me being overriden by some sort of other feature that handles how glotrot or ironshank is given
            else
                Biter.ApplyEffect(new IronshankOnset());
            return Endings.VOMIT;
        }

        public Endings LiquidEnding() //caves of qud certainly is a complex liquid simulation... luckily were just dealing with a micro part of it
        {
            List<Endings> liquidresults = new();
            if (BadLiquids["acid"])
                liquidresults.Add(AcidLiquid());
            if (BadLiquids["asphalt"])
                liquidresults.Add(AsphaltLiquid());
            if (BadLiquids["oil"])
                liquidresults.Add(OilLiquid());
            if (BadLiquids["slime"])
                liquidresults.Add(SlimeLiquid());
            if (BadLiquids["sludge"])
                liquidresults.Add(SludgeLiquid());
            if (BadLiquids["goo"])
                liquidresults.Add(GooLiquid());
            if (BadLiquids["ooze"])
                liquidresults.Add(OozeLiquid());
            if (BadLiquids["putrid"])
                liquidresults.Add(GrossLiquid());
            return liquidresults.Count != 0 ? liquidresults.Min() : Endings.OUT_OF_RANGE;
            //liquid features are based on the same type of result youd get by drinking them, but sometimes with less damage
        }
    }
}