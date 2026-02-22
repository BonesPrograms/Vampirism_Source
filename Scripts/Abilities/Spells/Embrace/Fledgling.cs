
using System;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class Fledgling : IScribedPart
    {

        public string SireID;
       public  long TimeOfSiring;
        public bool HatesSire;
        public bool IsChildeOf(GameObject Target)
        {
            return Target.ID == SireID;
        }

        public Fledgling() //dont forget to sync vamp levels for fun
        {

        }

        public Fledgling(GameObject Sire, bool HatesSire) : this()
        {
            SireID = Sire.ID;
            TimeOfSiring = The.Game.Turns;
            this.HatesSire = HatesSire;
        }
    }

}

