using VampirismSys.Stealth;
using VampirismSys.Extensions;
using System;
using System.Security.Principal;

namespace XRL.World.Parts
{
    internal class WitnessCreatedListener : IPart
    {
        public override bool WantEvent(int ID, int Cascade)
        {
            return ID == EnteredCellEvent.ID;
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {   
            if (!Nightbeast.Witnesses?.ContainsKey(ParentObject) ?? false && (The.Player?.IsVampire() ?? false) && StealthCore.ValidSentient(ParentObject))
            {
                StealthCore.CheckValidity(ParentObject);
                Nightbeast.RunStealthSystem(); //spawning of new objects requires us to completely resift, recount, and re-evaluate the witness dictionary
            }                                   //though we do not need to re-sift the zone
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
    }
}