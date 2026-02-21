
using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class Fledgling : IScribedPart
    {
        public GameObject Sire;
        public bool HatesSire;
        public long TimeOfSiring = The.Game.Turns;

        public bool IsChildeOf(GameObject Target)
        {
            return Target == Sire;
        }

        public Fledgling() //dont forget to sync vamp levels for fun
        {

        }

        public Fledgling(GameObject Sire, bool HatesSire) : this()
        {
            this.Sire = Sire;
            this.HatesSire = HatesSire;
        }
    }

}

