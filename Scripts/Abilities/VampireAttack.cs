using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using Nexus.Properties;
using Nexus.Stealth;
using System.Collections.Generic;
using XRL.World.AI;

namespace Nexus.Attack
{
    /// <summary>
    /// Handles the logic for finalizing an attack and actually beginning the feed.
    /// </summary>
    class VampireAttack
    {
        readonly GameObject Target;
        readonly Vampirism Source;
        readonly string dice;
        readonly bool friendly;
        bool vampire;

        public VampireAttack(GameObject Target, Vampirism Source)
        {
            this.Target = Target;
            this.Source = Source;
            this.dice = Source.GetDamageDice();
            this.friendly = Target.IsFriendly(Source.ParentObject);
            vampire = Target.IsVampire();
        }
        public void Attack(bool frenzy)
        {
            Nightbeast n = Source.ParentObject.IsPlayer() ? Source.ParentObject.GetPart<Nightbeast>() : null;
            Target.ApplyEffect(new Vampires_Kiss(FEED.DURATION));
            if (!frenzy && !friendly && (n?.ValidateStealthATK(Target) ?? false) && SpotterCheck(n))
                StealthATK();
            else
                CombatFeed(frenzy);
        }

        bool SpotterCheck(Nightbeast n)
        {
            if (SpotterGenerator.GeneratorWithDefaultList(n).BeginAttackCheckIfSpotted<OpinionDominate>(out GameObject spotter) == Spot.SPOTTER_IN_DETECTION)
            {
                Alert alert = Alert.AlertWithDefaultList(n, spotter);
                alert.Add(Target);
                alert.RemoveSleepFromWitnesses();
                alert.AddOpinionToWitnessesAndExposer<OpinionDominate>();
                return false;
            }
            else
                return true;
        }
        void CombatFeed(bool frenzy)
        {
            Source.BiteActivate(Target); //prevents prematurely humanity loss
            if (Target?.HasHitpoints() ?? false) //by making sure theyre alive after the bite
            {
                bool ghoul = Target.IsGhoulOf(Source.ParentObject);
                if (ghoul && Target.TryGetEffect(out Bleeding bleed) && bleed.Owner == Source.ParentObject)
                    Target.RemoveEffect(bleed);
                Source.ParentObject.ApplyEffect(new CombatFeed(Target, true, dice, FEED.DURATION, frenzy, friendly, ghoul, vampire));
                Target.ApplyEffect(new CombatFeed(Source.ParentObject, false, dice, FEED.DURATION, frenzy, friendly, ghoul, vampire));
            }

        }
        void StealthATK()
        {
            if (Source.ParentObject.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|You ambush " + Target.t() + " and}} {{B|silently}} {{G sequence|sink your fangs into " + Target.its + " neck.}}");
            Source.ParentObject.ApplyEffect(new StealthFeed(Target, true, dice, FEED.DURATION, vampire));
            Target.ApplyEffect(new StealthFeed(Source.ParentObject, false, dice, FEED.DURATION, vampire));
        }

    }
}