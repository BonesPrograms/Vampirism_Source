using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using Nexus.Core;
using XRL.World;

namespace Nexus.Biting
{

    public class LiquidBehaviors : VampireBite
    {
        public LiquidBehaviors(GameObject Biter) : base(Biter)
        {
        }
        Ending OilLiquid() //yeah drinking oil doesnt really do anything but its gross so i added it. used to make you puke but it doesnt do that ingame so i got rid of that
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("{{K sequence|Disgusting!}}.");
            return Ending.SUCCESS;
        }
        Ending AcidLiquid()
        {
            Biter.TakeDamage(WikiRng.Next(5, 10), "Acid", null, null);
            if (PainTolerance())
                return Ending.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{G sequence|IT BURNS!}}");
            return MakeSave("Bit Acidic Target");

        }

        Ending AsphaltLiquid()
        {
            Biter.TakeDamage(WikiRng.Next(5, 10), "Asphalt", null, null);
            Biter.TemperatureChange(+200);
            if (PainTolerance())
                return Ending.PAIN_TOLERANCE;
            if (Biter.IsPlayer())
                Popup.ShowFail("{{K sequence|It burns!}}");
            return MakeSave("Bit Flaming Target");
        }

        Ending GrossLiquid() //this ones kind of funny it just makes you puke. automatic failure
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's disgusting!");
            return Ending.VOMIT;
        }

        Ending SlimeLiquid() //favorite one because youre most likely to encounter slime covered enemies compared to any of these other liquids
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("Its disgustingly slimy!");
            Biter.ApplyEffect(new Confused(WikiRng.Next(6, 12), 5, 7)); //and its probably one of the worst effects on the list aside from disease
            return Ending.FAIL;
        }

        Ending SludgeLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's horrifying! You feel sick!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(8, 12), $"{WikiRng.Next(4, 6)}", 10));
            return Ending.VOMIT;
        }

        Ending GooLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("its awful! You feel poison coursing through your veins!");
            Biter.ApplyEffect(new Poisoned(WikiRng.Next(8, 12), $"{WikiRng.Next(4, 6)}", 10));
            return Ending.VOMIT;

        }

        Ending OozeLiquid()
        {
            if (Biter.IsPlayer())
                Popup.ShowFail("It's repulsive! You feel sick!");
            if (WikiRng.Next(1, 2) == 1) //funny thing about this during testing i found that even if i directly apply glotrot w/o rng, it may apply ironshank. wierd behavior, not sure if it was my fault, or
                Biter.ApplyEffect(new GlotrotOnset()); //me being overriden by some sort of other feature that handles how glotrot or ironshank is given
            else
                Biter.ApplyEffect(new IronshankOnset());
            return Ending.VOMIT;
        }
        public Ending LiquidEnding((string, bool)[] BadLiquids) //caves of qud certainly is a complex liquid simulation... luckily were just dealing with a micro part of it
        {
            Ending[] array = new Ending[BadLiquids.CapacityByValue(true)];
            int index = 0;
            if (BadLiquids[0].Item2)
            {
                array[index] = SludgeLiquid();
                index++;
            }
            if (BadLiquids[1].Item2)
            {
                array[index] = OozeLiquid();
                index++;
            }
            if (BadLiquids[2].Item2)
            {
                array[index] = GooLiquid();
                index++;
            }
            if (BadLiquids[3].Item2)
            {
                array[index] = OilLiquid();
                index++;
            }
            if (BadLiquids[4].Item2)
            {
                array[index] = AcidLiquid();
                index++;
            }
            if (BadLiquids[5].Item2)
            {
                array[index] = SlimeLiquid();
                index++;
            }
            if (BadLiquids[6].Item2)
            {
                array[index] = GrossLiquid();
                index++;
            }
            if (BadLiquids[7].Item2)
                array[index] = AsphaltLiquid();
            return Result(array);
            //liquid features are based on the same type of result youd get by drinking them, but sometimes with less damage
        }
    }
}