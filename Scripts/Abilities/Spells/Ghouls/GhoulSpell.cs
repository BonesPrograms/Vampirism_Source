using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;
using XRL.World.Parts.Mutation;
using Nexus.Blood;

namespace XRL.World.Parts
{

    [Serializable]
    public class GhoulSpell : BaseVampireSpell
    {
        public override int Cooldown => Nexus.Rules.Ghoul.COOLDOWN;
        const string TEXT = "to enthrall";
        public GhoulSpell()
        {
            CommandName = Nexus.Rules.Ghoul.COMMAND_NAME;
            AbilityMenuName = Nexus.Rules.Ghoul.ABILITY_NAME;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == AfterDieEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == Nexus.Rules.Ghoul.COMMAND_NAME && Checks.Prerequisites(base.ParentObject, Nexus.Rules.Ghoul.ABILITY_NAME, TEXT))
            {
                if (base.ParentObject.TryGetTarget(AbilityMenuName, TEXT, out GameObject pick) && Checks.Attackable(pick, TEXT) && NotAlreadyFollower(pick))
                    MakeAttack(pick);

            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterDieEvent E)
        {
            RemoveLastGhoul();
            return base.HandleEvent(E);
        }
        public bool NotAlreadyFollower(GameObject pick) //for now - i have problems with you trying to mix and match these effects
        {
            if (pick.InSamePartyAs(ParentObject))
            {
                XRL.UI.Popup.Show($"{pick.t()} is already your follower.");
                return false;
            }
            return true;
        }

        public void MakeAttack(GameObject Target)
        {
            if (!ParentObject.IsRealityDistortionUsable())
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
            else if (!IsVampire(Target))
                Cast(Target);
        }

        bool IsVampire(GameObject Target)
        {
            if (Target.IsVampire())
            {
                UI.Popup.Show("You cannot enthrall other vampires.");
                return true;
            }
            return false;
        }

        bool AlreadyEnthralled(GameObject Target, out EnthralledGhoul e)
        {
            e = Target.GetEffect<EnthralledGhoul>();
            return e != null;
        }

        void CheckEnthrallment(EnthralledGhoul e)
        {
            if (e.Master == ParentObject)
            {
                e.Metab.StatusToString(out BloodLevel level);
                if (level == BloodLevel.GLUT)
                    AddPlayerMessage($"{e.Object.t()} is already gorged on " + "{{r|blood}}.");
                else
                    ExpendBlood(e, false);
            }
            else
                UI.Popup.Show($"{e.Object.t()} already has a master.");

        }

        void ExpendBlood(EnthralledGhoul e, bool showPopup)
        {
            e.Buff(Roll());
            string basemessage = $"You feed {e.Object.t()} your blood";
            string output = showPopup == true ? $"{basemessage} and enthrall their mind." : $"{basemessage}.";
            base.ExpendBlood(showPopup, output);
        }


        void Cast(GameObject Target)
        {
            if (AlreadyEnthralled(Target, out var e))
                CheckEnthrallment(e);
            else if (base.Cast(TEXT) && RealityCheck(Target.CurrentCell) && Attack(Target))
            {
                RemoveLastGhoul();
                var ghoul = new EnthralledGhoul(ParentObject);
                Target.ApplyEffect(ghoul);
                ExpendBlood(ghoul, true);
            }
        }
        bool Attack(GameObject Target) =>
        Capabilities.Mental.PerformAttack(Enthrall, base.ParentObject, Target, null, Nexus.Rules.Ghoul.COMMAND_NAME, "1d8", 1, int.MinValue, int.MinValue, base.Roll(), Target.Stat("Level"));

        void RemoveLastGhoul()
        {
            foreach (var obj in ParentObject.Brain.PartyMembers.ToArray())
            {
                var ghoul = obj.Value.Reference?.Object;
                if (ghoul?.RemoveEffect<EnthralledGhoul>() ?? false)
                {
                    ParentObject.Brain.PartyMembers.Remove(obj.Key);
                    return;
                }
            }
        }

        bool Enthrall(MentalAttackEvent E)
        {
            GameObject defender = E.Defender;
            if (E.Penetrations <= 0)
            {
                AddPlayerMessage("{{R|" + defender.t() + "resists your attempts to enthrall their mind!}}");
                defender.AddOpinion<OpinionDominate>(E.Attacker);
                return false;
            }

            return true;
        }
        public override void CollectStats(Templates.StatCollector stats)
        {
            int num = Math.Max(ParentObject.StatMod("Ego"), Level + ParentObject.GetStat("Level").Value);
            switch (num)
            {
                case 0:
                    stats.Set("Attack", "1d8", !stats.mode.Contains("ability"));
                    break;
                case > 0:
                    stats.Set("Attack", "1d8+" + num, !stats.mode.Contains("ability"));
                    break;
                default:
                    stats.Set("Attack", "1d8" + num, !stats.mode.Contains("ability"));
                    break;
            }
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Nexus.Rules.Ghoul.COOLDOWN);
        }

        public override void RemoveSpell()
        {
            RemoveLastGhoul();
            base.RemoveSpell();
        }
    }
}