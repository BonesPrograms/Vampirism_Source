using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using Nexus.Properties;
using System.Collections.Generic;

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
        Command Send(Nightbeast n, bool frenzy, bool feedingOnCompanion, bool StealthValidated)
        {
            if (n is not null && !frenzy && !feedingOnCompanion && StealthValidated)
            {
                if (!n.BeginAttackCheckIfSpotted<XRL.World.AI.OpinionDominate>(out GameObject Spotter))
                    return StealthATK();
                
            }
            return CombatFeed();
        }

        public Command Attack()
        {
            GatherData(out bool feedingOnCompanion, out bool frenzy, out Nightbeast n);
            Target.ApplyEffect(new Vampires_Kiss(FEED.DURATION));
            return Send(n, frenzy, feedingOnCompanion, n.BeforeAttackValidate(Target));
        }
        Command CombatFeed()
        {
            Source.BiteActivate(Target); //prevents prematurely humanity loss
            if (Target?.HasHitpoints() is true) //by making sure theyre alive after the bite
            {
                Source.ParentObject.ApplyEffect(new CombatFeed(Target, true, Source.Level, Source.GetDamageDice(Source.Level), FEED.DURATION));
                Target.ApplyEffect(new CombatFeed(Source.ParentObject, false, Source.Level, Source.GetDamageDice(Source.Level), FEED.DURATION));
            }
            return Command.END;

        }
        Command StealthATK()
        {
            if (Source.ParentObject.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|You ambush " + Target.t() + " and}} {{B|silently}} {{G sequence|sink your fangs into " + Target.its + " neck.}}");
            Source.ParentObject.ApplyEffect(new StealthFeed(Target, true, Source.Level, Source.GetDamageDice(Source.Level), FEED.DURATION));
            Target.ApplyEffect(new StealthFeed(Source.ParentObject, false, Source.Level, Source.GetDamageDice(Source.Level), FEED.DURATION));
            return Command.END;
        }
        void GatherData(out bool feedingOnCompaion, out bool frenzy, out Nightbeast n)
        {
            feedingOnCompaion = Scan.IsFriendly(Target, Source.ParentObject);
            frenzy = Scan.SafeReturnProperty(Source.ParentObject, Flags.FRENZY);
            n = Source.ParentObject.IsPlayer() ? Source.ParentObject.GetPart<Nightbeast>() : null;
        }




    }
}