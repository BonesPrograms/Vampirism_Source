using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{
    public class CoffinSpell : VampiricSpell
    {

        public GameObject Coffin;
        public Guid CoffinActivatedAbilityID = Guid.Empty;
        public override bool ShouldSync() => true;
        public override void RequireObject()
        {
            CoffinActivatedAbilityID = AddMyActivatedAbility("coffin spell", "coffin spell", "Vampiric Spells", null, "\u009f");
        }

        public override void RemoveObject()
        {
        RemoveMyActivatedAbility(ref CoffinActivatedAbilityID);
        ParentObject.RemovePart(this);
        }
    }
}