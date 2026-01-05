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

        public VampireAttack(GameObject Target, Vampirism Source)
        {
            this.Target = Target;
            this.Source = Source;
        }
        public void Attack(bool frenzy)
        {
            Nightbeast n = Source.ParentObject.IsPlayer() ? Source.ParentObject.GetPart<Nightbeast>() : null;
            Target.ApplyEffect(new Vampires_Kiss(FEED.DURATION));
            bool friendly = Scan.IsFriendly(Target, Source.ParentObject);
            if (!frenzy && !friendly && (n?.ValidateStealthATK(Target) ?? false) && SpotterCheck(n))
                StealthATK();
            else
                CombatFeed(frenzy, friendly);
        }

        bool SpotterCheck(Nightbeast n)
        {
            if (new SpotterGenerator(n, SpotterGenerator.GiveDefaultList(n)).BeginAttackCheckIfSpotted<OpinionDominate>(out GameObject spotter) == Spot.SPOTTER_IN_DETECTION)
            {
                Alert alert = new(n, Alert.GiveDefaultList(n), spotter);
                alert.SafeAdd(Target);
                alert.RemoveSleepFromWitnesses();
                alert.AddOpinionToWitnessesAndExposer<OpinionDominate>();
                return false;
            }
            else
                return true;
        }
        void CombatFeed(bool frenzy, bool friendly)
        {
            Source.BiteActivate(Target); //prevents prematurely humanity loss
            if (Target?.HasHitpoints() ?? false) //by making sure theyre alive after the bite
            {
                Source.ParentObject.ApplyEffect(new CombatFeed(Target, true, Source.Level, Vampirism.GetDamageDice(Source.Level), FEED.DURATION, frenzy, friendly));
                Target.ApplyEffect(new CombatFeed(Source.ParentObject, false, Source.Level, Vampirism.GetDamageDice(Source.Level), FEED.DURATION, frenzy, friendly));
            }

        }
        void StealthATK()
        {
            if (Source.ParentObject.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|You ambush " + Target.t() + " and}} {{B|silently}} {{G sequence|sink your fangs into " + Target.its + " neck.}}");
            Source.ParentObject.ApplyEffect(new StealthFeed(Target, true, Source.Level, Vampirism.GetDamageDice(Source.Level), FEED.DURATION));
            Target.ApplyEffect(new StealthFeed(Source.ParentObject, false, Source.Level, Vampirism.GetDamageDice(Source.Level), FEED.DURATION));
        }

    }
}