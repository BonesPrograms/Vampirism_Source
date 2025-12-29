using XRL.UI;
using XRL.World.Effects;
using XRL.World.AI;
using Nexus.Properties;
using XRL.World.Parts.Mutation;
using XRL.World.Parts;
using XRL.World;
using Nexus.Bite;
using XRL.Rules;
using System;
using Nexus.Core;
using Nexus.Rules;


namespace Nexus.Attack
{

    enum Command
    {
        END,
        CONTINUE
    }


    /// <summary>
    /// Brings together the Vampire's property values and parts' methods for various evaluations before a Feed can begin.
    /// </summary>

    class HandleCommand
    {
        bool friends;
        readonly bool badtarget;
        readonly GameObject Target;
        readonly Vampirism Source;
        readonly Biting Bite;

        public HandleCommand(GameObject Target, Vampirism Source, bool badtarget, Biting Bite)
        {
            this.Target = Target;
            this.Source = Source;
            this.Bite = Bite;
            this.badtarget = badtarget;
        }

        Command CheckStealth() => !Scan.SafeReturnProperty(Source.ParentObject, Flags.STEALTH) ? ResistAttack() : Command.CONTINUE;
        Command CheckVulnerabilities() => Scan.Vulnerability(Target, Source.ParentObject) ? BeginAttack() : Command.CONTINUE;
        Command Sharing() => Target.HasEffectDescendedFrom<IFeeding>() ? CheckSharing() : Command.CONTINUE;
        Command ResistAttack() => ParentRoll < TargetRoll ? ShowFailure(Target) : Command.CONTINUE;
        int ParentRoll => WikiRng.Next(1, 8) + Math.Max(Source.ParentObject.StatMod("Agility"), Source.Level) + Source.ParentObject.GetStat("Level").Value;
        int TargetRoll => Stats.GetCombatDV(Target) + Target.GetStat("Level").Value;

        /// <summary>
        /// Begins HandleCommand method chain.
        /// </summary>
        public void Initialize()
        {
            if (BeforeAttack() == Command.END)
                return;
            Source.UseEnergy(1000, "Physical Mutation Vampirism");
            Source.CooldownMyActivatedAbility(Source.FangsActivatedAbilityID, Source.GetCooldown(Source.Level));
            if (CheckVulnerabilities() == Command.END)
                return;
            if (CheckStealth() == Command.END)
                return;
            BeginAttack();
        }
        Command BeforeAttack()
        {
            if (Attackable() == Command.END)
                return Command.END;
            if (Sharing() == Command.END)
                return Command.END;
            if (Warnings() == Command.END)
                return Command.END;
            return Command.CONTINUE;
        }

        Command Attackable()
        {
            if (!Scan.Applicable(Target)) //invalid targets are those not from the animal kingdom
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail("You cannot feed from " + Target.t() + ".");
                return Command.END;
            }
            if (Target.IsFrozen()) //cant bite ice block people
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail(Target.t() + " is frozen solid!");
                return Command.END;
            }
            if (Target.IsInStasis())
            {
                if (Source.ParentObject.IsPlayer())
                    Popup.ShowFail(Target.t() + " is in stasis.");
                return Command.END;
            }
            return Command.CONTINUE;

        }

        Command Warnings()
        {
            if (badtarget)
            {
                if (Source.ParentObject.IsPlayer())
                    if (Popup.ShowYesNo(Target.t() + " looks gross. Are you sure you want to bite " + Target.them + "?") is DialogResult.No)
                        return Command.END;
            }
            if (Source.ParentObject.GetIntProperty(Flags.HUMANITY) is Rules.HUMANITY.CRIT)
            {
                if (Popup.ShowYesNo("Your {{G sequence|Humanity}} is {{R|CRITICAL!}}\nAre you sure you want to feed on " + Target.t() + "?") is DialogResult.No)
                    return Command.END;
            }
            if (Source.ParentObject.GetIntProperty(Flags.BLOOD_VALUE) >= VITAE.FEED_PUKE_WARN && Source.ParentObject.IsPlayer())
            {
                if (Source.ParentObject.GetPart<Vitae>().IDontWantToPuke(true))
                    return Command.END;
            }
            return Command.CONTINUE;
        }



        Command CheckSharing()
        {
            GameObject Feeder = Target.GetEffectDescendedFrom<IFeeding>().other.Object;
            if (!Scan.Unaware(Feeder, false) && !Scan.IsFriendly(Feeder, Source.ParentObject))
                return NoSharing(Feeder);
            else
            {
                friends = true;
                return Command.CONTINUE;
            }
        }

        Command NoSharing(GameObject Feeder)
        {
            if (Source.ParentObject.IsPlayer())
                Popup.ShowFail(Feeder.t() + " is already feeding on " + Target.t() + ", and " + Feeder.it + " doesn't want to share.");
            if (!Scan.Unaware(Target, false) && !Scan.IsFriendly(Target, Source.ParentObject))
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.AddOpinion<OpinionDominate>(Source.ParentObject);
            Feeder.RemoveEffectDescendedFrom<IFeeding>();
            return Command.END;
        }

        Command BeginAttack()
        {
            if (badtarget && Bite.CannotFeed())
                return Command.END;
            if (friends && Source.ParentObject.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("{{R|Sharing is caring.}}");
            return new VampireAttack(Target, Source).Attack();
        }
        Command ShowFailure(GameObject Target)
        {
            if (Source.ParentObject.IsPlayer())
                IComponent<GameObject>.XDidYToZ(Target, "resist", Source.ParentObject, "vampiric bite", "!", null, null, null, Source.ParentObject, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: false, IndefiniteObjectForOthers: false, PossessiveObject: true, null, null, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: false, AlwaysVisible: false, FromDialog: true);
            else if (Target.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You resist " + Source.ParentObject.poss("vampiric bite!") + "!", 'g');
            else
                IComponent<GameObject>.XDidYToZ(Target, "resist", Source.ParentObject, "vampiric bite", "!", null, null, null, Target);
            if (!Target.IsPlayer())
                Target.AddOpinion<OpinionDominate>(Source.ParentObject);
            return Command.END;
        }


    }
}