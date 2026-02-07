using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using Nexus.Properties;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using Nexus.Biting;
using XRL.Rules;
using System;
using Nexus.Core;
using Nexus.Rules;


namespace Nexus.Attack
{
    /// <summary>
    /// Brings together the Vampire's property values and parts' methods for various evaluations before a Feed can begin.
    /// </summary>

    public class FeedCommand
    {
        bool friends;
        readonly Vampirism Source;
        public readonly Bite Bite;
        bool badtarget;
        public bool AutoWin = false;
        public FeedCommand(Vampirism Source)
        {
            this.Source = Source;
            Bite = new Bite(Source.ParentObject, Source);
        }

        bool Stealth => Source.ParentObject.CheckFlag(FLAGS.STEALTH);
        //this has nothing to do with whether or not you get a Stealth Feed it is just for skipping attack resistance. VampireAttack evaluates stealth separately
        //using stricter logic from a method in Nightbeast that enforces rules related to the "one witness" feature for stealth, as well as Spotter features
        int ParentRoll => WikiRng.Next(1, 8) + Math.Max(Source.ParentObject.StatMod("Agility"), Source.Level) + Source.ParentObject.GetStat("Level").Value;
        int TargetRoll(GameObject Target) => Stats.GetCombatDV(Target) + Target.GetStat("Level").Value;
        bool Success(GameObject Target) => Checks.Vulnerability(Target, Source.ParentObject) || Stealth || ParentRoll > TargetRoll(Target);

        /// <summary>
        /// Begins HandleCommand method chain.
        /// </summary>
        public void Initialize(GameObject Target)
        {
            if (BeforeAttackCheckIfValid(Target))
            {
                Source.UseEnergy(1000, "Physical Mutation Vampirism");
                Source.CooldownMyActivatedAbility(Source.FangsActivatedAbilityID, FEED.COOLDOWN);
                if (Success(Target) || AutoWin)
                    BeginAttack(Target);
                else
                    ShowFailure(Target);
            }

        }
        bool BeforeAttackCheckIfValid(GameObject Target)
        {
            if (!Checks.Attackable(Target, "feed from") || !Warnings(Target))
                return false;
            else if (Target.TryGetEffect(out IFeeding feed))
            {
                GameObject Feeder = feed.other.Object;
                return Feeder.IsFriendly(Source.ParentObject) ? friends = true : NoSharing(Feeder, Target);
            }
            else
                return true;
        }

        bool Warnings(GameObject Target)
        {
            if (badtarget = Bite.BadTarget(Target))
            {
                if (Source.ParentObject.IsPlayer())
                    if (Popup.ShowYesNo(Target.t() + " looks gross. Are you sure you want to bite " + Target.them + "?") == DialogResult.No)
                        return false;
            }
            if (Source.ParentObject.GetIntProperty(FLAGS.HUMANITY) == Rules.HUMANITY.CRIT)
            {
                if (Popup.ShowYesNo("Your {{G sequence|Humanity}} is {{R|CRITICAL!}}\nAre you sure you want to feed on " + Target.t() + "?") == DialogResult.No)
                    return false;
            }
            if (Source.ParentObject.GetIntProperty(FLAGS.BLOOD_VALUE) >= VITAE.FEED_PUKE_WARN && Source.ParentObject.IsPlayer())
            {
                if (Source.ParentObject.GetPart<Vitae>().IDontWantToPuke(true, false))
                    return false;
            }
            return true;
        }

        bool NoSharing(GameObject Feeder, GameObject Target)
        {
            Popup.ShowFail(Feeder.t() + " is already feeding on " + Target.t() + ", and " + Feeder.it + " doesn't want to share.");
            if (!Target.Unaware(true) && !Target.IsFriendly(Source.ParentObject))
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.RemoveEffectDescendedFrom<IFeeding>();
            return false;
        }

        void BeginAttack(GameObject Target)
        {
            if (!badtarget || !Bite.CannotFeed(Target))
            {
                if (friends && Source.ParentObject.IsPlayer())
                    IComponent<GameObject>.AddPlayerMessage("{{R|Sharing is caring.}}");
                new VampireAttack(Target, Source, Source.GetDamageDice(), Target.IsFriendly(Source.ParentObject)).Attack(false);
            }
        }
        void ShowFailure(GameObject Target)
        {
            if (Source.ParentObject.IsPlayer())
                IComponent<GameObject>.XDidYToZ(Target, "resist", Source.ParentObject, "vampiric bite", "!", null, null, null, Source.ParentObject, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: false, IndefiniteObjectForOthers: false, PossessiveObject: true, null, null, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: false, AlwaysVisible: false, FromDialog: true);
            else if (Target.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You resist " + Source.ParentObject.poss("vampiric bite!") + "!", 'g');
            else
                IComponent<GameObject>.XDidYToZ(Target, "resist", Source.ParentObject, "vampiric bite", "!", null, null, null, Target);
            if (!Target.IsPlayer())
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
        }


    }
}