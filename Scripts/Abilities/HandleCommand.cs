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

    class HandleCommand
    {
        bool friends;
        readonly GameObject Target;
        readonly Vampirism Source;
        readonly Bite Bite;
        bool badtarget;

        public HandleCommand(GameObject Target, Vampirism Source, Bite Bite)
        {
            this.Target = Target;
            this.Source = Source;
            this.Bite = Bite;
        }

        bool Stealth => Scan.ReturnProperty(Source.ParentObject, Flags.STEALTH);
        //this has nothing to do with whether or not you get a Stealth Feed it is just for skipping attack resistance. VampireAttack evaluates stealth separately
        //using stricter logic from a method in Nightbeast that enforces rules related to the "one witness" feature for stealth, as well as Spotter features
        int ParentRoll => WikiRng.Next(1, 8) + Math.Max(Source.ParentObject.StatMod("Agility"), Source.Level) + Source.ParentObject.GetStat("Level").Value;
        int TargetRoll => Stats.GetCombatDV(Target) + Target.GetStat("Level").Value;
        bool Success => Scan.Vulnerability(Target, Source.ParentObject) || Stealth || ParentRoll > TargetRoll;

        /// <summary>
        /// Begins HandleCommand method chain.
        /// </summary>
        public void Initialize()
        {
            if (BeforeAttackCheckIfValid())
            {
                Source.UseEnergy(1000, "Physical Mutation Vampirism");
                Source.CooldownMyActivatedAbility(Source.FangsActivatedAbilityID, Vampirism.GetCooldown(Source.Level));
                if (Success)
                    BeginAttack();
                else
                    ShowFailure(Target);
            }

        }
        bool BeforeAttackCheckIfValid()
        {
            if (!Attackable() || !Warnings())
                return false;
            else if (Target.HasEffectDescendedFrom<IFeeding>())
                return CheckSharing();
            else
                return true;
        }

        bool Attackable()
        {
            if (!Scan.Applicable(Target)) //invalid targets are those not from the animal kingdom
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail("You cannot feed from " + Target.t() + ".");
                return false;
            }
            if (Target.IsFrozen()) //cant bite ice block people
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail(Target.t() + " is frozen solid!");
                return false;
            }
            if (Target.IsInStasis())
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail(Target.t() + " is in stasis.");
                return false;
            }
            return true;

        }

        bool Warnings()
        {
            if (badtarget = Bite.BadTarget())
            {
                if (Source.ParentObject.IsPlayer())
                    if (Popup.ShowYesNo(Target.t() + " looks gross. Are you sure you want to bite " + Target.them + "?") == DialogResult.No)
                        return false;
            }
            if (Source.ParentObject.GetIntProperty(Flags.HUMANITY) == Rules.HUMANITY.CRIT)
            {
                if (Popup.ShowYesNo("Your {{G sequence|Humanity}} is {{R|CRITICAL!}}\nAre you sure you want to feed on " + Target.t() + "?") == DialogResult.No)
                    return false;
            }
            if (Source.ParentObject.GetIntProperty(Flags.BLOOD_VALUE) >= VITAE.FEED_PUKE_WARN && Source.ParentObject.IsPlayer())
            {
                if (Source.ParentObject.GetPart<Vitae>().IDontWantToPuke(true))
                    return false;
            }
            return true;
        }



        bool CheckSharing()
        {
            GameObject Feeder = Target.GetEffectDescendedFrom<IFeeding>().other.Object;
            return Scan.IsFriendly(Feeder, Source.ParentObject) ? friends = true : NoSharing(Feeder);
        }

        bool NoSharing(GameObject Feeder)
        {
            if (Source.ParentObject.IsPlayer())
                Popup.ShowFail(Feeder.t() + " is already feeding on " + Target.t() + ", and " + Feeder.it + " doesn't want to share.");
            if (!Scan.Unaware(Target, true) && !Scan.IsFriendly(Target, Source.ParentObject))
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.RemoveEffectDescendedFrom<IFeeding>();
            return false;
        }

        void BeginAttack()
        {
            if (!badtarget || !Bite.CannotFeed())
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