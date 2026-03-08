using Nexus.Stealth;
using Nexus.Core;

namespace XRL.World.Parts
{
    public class WitnessCreatedListener : IPart
    {

        //whats nice about this: its compatible with old saves
        //the only function of this part is to notify the player's stealth part that an object has been created in the zone and needs to be added to the witness dictionary
        //so by very nature it will always be new objects with this part
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == AfterObjectCreatedEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {   //this runs before the game even begins so you need a null check
            if (The.Player?.IsVampire() ?? false && ParentObject.InSameZone(The.Player) && StealthCore.ValidSentient(ParentObject))
            {
                Check();
            }
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }

        void Check()
        {
            Nightbeast.Witnesses[ParentObject] = StealthCore.NearbySentient(ParentObject) && StealthCore.ActiveWitness(ParentObject);
        }
    }
}