using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using VampirismSys.Properties;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using VampirismSys.Biting;
using XRL.Rules;
using System;
using VampirismSys.Core;
using VampirismSys.Rules;


namespace VampirismSys.Attack
{
    /// <summary>
    /// Brings together the Vampire's property values and parts' methods for various evaluations before a Feed can begin.
    /// </summary>

    internal class FeedAbility 
    {
        bool friends;
        readonly Vampirism Source;
        internal readonly Bite Bite;
        bool badtarget;
        internal static bool AutoWin;
        internal FeedAbility(Vampirism Source)
        {
            this.Source = Source;
            Bite = new Bite(Source.ParentObject, Source);
        }

        bool Stealth => Source.ParentObject.CheckFlag(Flags.STEALTH);
        //this has nothing to do with whether or not you get a Stealth Feed it is just for skipping attack resistance. VampireAttack evaluates stealth separately
        //using stricter logic from a method in Nightbeast that enforces rules related to the "one witness" feature for stealth, as well as Spotter features
        int ParentRoll => WikiRng.Next(1, 8) + Math.Max(Source.ParentObject.StatMod("Agility"), Source.Level) + Source.ParentObject.GetStat("Level").Value;
        int TargetRoll(GameObject Target) => Stats.GetCombatDV(Target) + Target.GetStat("Level").Value;
        bool Success(GameObject Target) => Checks.Vulnerability(Target, Source.ParentObject) || Stealth || ParentRoll > ProcessTargetRoll(Target);

        int ProcessTargetRoll(GameObject Target)
        {
            int value = TargetRoll(Target);
            if (Target.IsConfused || Target.HasEffect<Dazed>())
                value /= 2;
            return value;
        }

        /// <summary>
        /// Begins HandleCommand method chain.
        /// </summary>
        internal void Initialize(GameObject Target)
        {
            if (BeforeAttackCheckIfValid(Target))
            {
                Source.UseEnergy(1000, "Physical Mutation Vampirism");
                Source.CooldownMyActivatedAbility(Source.FangsActivatedAbilityID, Feed.COOLDOWN);
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
            else if (Target.TryGetEffect(out BaseFeedEffect feed)) //this block of code determiens the outcome if you try to interfere with another vampires feed
            {
                GameObject Feeder = feed.other.Object;
                friends = Feeder.IsFriendly(Source.ParentObject);
                return friends || NotFriendly(Feeder, Target);
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
            if (Source.ParentObject.GetIntProperty(Flags.HUMANITY) == Rules.Humanity.CRIT)
            {
                if (Popup.ShowYesNo("Your {{G sequence|Humanity}} is {{R|CRITICAL!}}\nAre you sure you want to feed on " + Target.t() + "?") == DialogResult.No)
                    return false;
            }
            if (Source.ParentObject.GetIntProperty(Flags.BLOOD_VALUE) >= Rules.Vitae.FEED_PUKE_WARN && Source.ParentObject.IsPlayer())
            {
                if (Source.ParentObject.GetPart<XRL.World.Parts.VampireBloodMetabolism>().PukeWarning(true))
                    return false;
            }
            return true;
        }

        bool NotFriendly(GameObject Feeder, GameObject Target)
        {
            Popup.ShowFail(Feeder.t() + " is already feeding on " + Target.t() + ", and " + Feeder.it + " doesn't want to share.");
            if (!Target.Unaware(true) && !Target.IsFriendly(Source.ParentObject))
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.RemoveEffectDescendedFrom<BaseFeedEffect>();
            return false;
        }

        void BeginAttack(GameObject Target)
        {
            if (!badtarget || !Bite.CannotFeed(Target))
            {
                if (friends && Source.ParentObject.IsPlayer())
                    IComponent<GameObject>.AddPlayerMessage("{{R|Sharing is caring.}}");
                new VampireAttack(Target, Source).Attack(false);
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