using System;
using VampirismSys.Properties;
using VampirismSys.Death;
using VampirismSys.Extensions;
using VampirismSys.Rules;
using XRL.UI;
using System.Collections.Generic;
using System.Linq;
using VampirismSys.Core;

namespace XRL.World.Parts
{
    /// <summary>
    /// The external part held by all edible targets in the world. Watches for the object's conditions on death - deducts humanity if the player performs an action that violates the rules of humanity.
    /// </summary>
    [Serializable]
    public class DeathHandler : IPart
    {

        //instead of the gameobject that is "really" them 
        public bool FinishedInit;                                   //meaning: we cant find the humanity part, and innocence becomes relative to whatever gameobject the player is currently dominating
        public override bool WantEvent(int ID, int cascade)     //so you could dominate a snapjaw, and load a zone with snapjaws, and then come back as the original player
        {                                                       //start feeding on them and then lose humanity because they have the innocent flag
            if (ID == SingletonEvent<BeforeTakeActionEvent>.ID) //(for various reasons, checking hostility on death doesnt work)
                return !FinishedInit;
            if (ID == TookDamageEvent.ID)
                return Options.GetOptionBool(ModOptions.FRACTUS_NERF);
            if (ID == DeathEvent.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(TookDamageEvent E)
        {
            if (E.Object == ParentObject && (ParentObject.CurrentCell?.HasObjectWithPart(nameof(Fracti)) ?? false))
                Saltify.Salt(ParentObject.CurrentCell);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            FinishedInit = Init.Evaluate(ParentObject, PlayerFinder.Player);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DeathEvent E)
        {
            bool isvampire = E.Dying.IsVampire();
            if (E.Dying.CurrentCell != null)
                MarkOnDeath.Check(E.Dying, isvampire);
            if (!isvampire)
                EvilActs.Check(E.Killer, E.Dying);
            return base.HandleEvent(E);
        }

    }

}