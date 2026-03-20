using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using VampirismSys.Extensions;
using VampirismSys.Rules;
using VampirismSys.Properties;
using VampirismSys.Stealth;
using System.Collections.Generic;
using XRL.World.AI;

namespace VampirismSys.Attack
{
    /// <summary>
    /// Handles the logic for finalizing an attack and actually beginning the feed.
    /// </summary>
    public class VampireAttack
    {
        readonly GameObject Target;
        readonly Vampirism Source;
        readonly string dice;
        readonly bool friendly;
        readonly bool vampire;

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
            Target.ApplyEffect(new VampiresKiss(Source.ParentObject));
            if (Source.ParentObject.IsPlayer() && Nightbeast.Stealthed && !frenzy && !friendly && SpotterCheck())
                StealthATK();
            else
                CombatFeed(frenzy);
        }

        bool SpotterCheck()
        {
            if (new SpotterCore(Source.ParentObject).Check<OpinionDominate>(out GameObject spotter))
            {
                Alert alert = new(Source.ParentObject, spotter);
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
                Source.ParentObject.ApplyEffect(new CombatFeed(Target, true, dice, frenzy, friendly, ghoul, vampire));
                Target.ApplyEffect(new CombatFeed(Source.ParentObject, false, dice,  frenzy, friendly, ghoul, vampire));
            }

        }
        void StealthATK()
        {
            IComponent<GameObject>.AddPlayerMessage("{{G sequence|You ambush " + Target.t() + " and}} {{B|silently}} {{G sequence|sink your fangs into " + Target.its + " neck.}}");
            Source.ParentObject.ApplyEffect(new StealthFeed(Target, true, dice,  vampire));
            Target.ApplyEffect(new StealthFeed(Source.ParentObject, false, dice, vampire));
        }

    }
}