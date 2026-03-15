using System;
using XRL.World.Effects;
using VampirismSys.Core;
using VampirismSys.Rules;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;
using XRL.World.Parts.Mutation;
using VampirismSys.Blood;

namespace XRL.World.Parts
{

    [Serializable]
    public class GhoulSpell : BaseVampireSpell
    {
        protected override int Cooldown => VampirismSys.Rules.Ghoul.COOLDOWN;
        const string TEXT = "to enthrall";
        protected override int Roll() => WikiRng.Next(1, 8) + Math.Max(ParentObject.StatMod("Ego"), Level);
        public GhoulSpell()
        {
            CommandName = VampirismSys.Rules.Ghoul.COMMAND_NAME;
            AbilityMenuName = VampirismSys.Rules.Ghoul.ABILITY_NAME;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == AfterDieEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == VampirismSys.Rules.Ghoul.COMMAND_NAME && Checks.Prerequisites(base.ParentObject, VampirismSys.Rules.Ghoul.ABILITY_NAME, TEXT))
            {
                if (base.ParentObject.TryGetTarget(AbilityMenuName, TEXT, out GameObject pick) && Checks.Attackable(pick, TEXT))
                    MakeAttack(pick);

            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterDieEvent E)
        {
            RemoveLastGhoul(E.Dying);
            return base.HandleEvent(E);
        }

        public void MakeAttack(GameObject Target)
        {
            if (!ParentObject.IsRealityDistortionUsable())
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
            else if (!IsVampire(Target))
                Cast(Target);
        }
        void Cast(GameObject Target)
        {
            if (base.Cast(TEXT)) //used to do a reality check here but... i dont think feeding a ghoul needs a reality distortion check
            {
                if (AlreadyEnthralled(Target, out var ghoul))
                    CheckEnthrallment(ghoul);
                else if (NotAlreadyFollower(Target) && RealityCheck(Target, true) && Attack(Target))
                    Enthrall(Target);

            }
        }
        bool Attack(GameObject Target) =>
        Capabilities.Mental.PerformAttack
        (Enthrall, base.ParentObject, Target, null, VampirismSys.Rules.Ghoul.COMMAND_NAME, "1d8", 1, int.MinValue, int.MinValue, base.Roll(), Target.Stat("Level"));
        void Enthrall(GameObject Target)
        {
            RemoveLastGhoul(ParentObject);
            var ghoul = new EnthralledGhoul(ParentObject);
            Target.ApplyEffect(ghoul);
            ExpendBlood(ghoul, true);
            Target.ApplyEffect(ghoul);
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
        bool NotAlreadyFollower(GameObject pick) //for now - i have problems with you trying to mix and match these effects
        {
            if (pick.InSamePartyAs(ParentObject))
            {
                XRL.UI.Popup.Show($"{pick.t()} is already your follower.");
                return false;
            }
            return true;
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

        bool AlreadyEnthralled(GameObject Target, out EnthralledGhoul ghoul)
        {
            ghoul = Target.GetEffect<EnthralledGhoul>();
            return ghoul != null;
        }

        void CheckEnthrallment(EnthralledGhoul ghoul)
        {
            if (ghoul.IsGhoulOf(ParentObject))
            {
                if (RealityCheck(ghoul.Object, false))
                    ExpendBlood(ghoul, false);
            }
            else
                UI.Popup.Show($"{ghoul.Object.t()} already has a master.");

        }

        bool RealityCheck(GameObject ghoul, bool showPopup)
        {
            if (RealityCheck(ParentObject.CurrentCell))
                return true;
            base.ExpendBlood(showPopup, $"You feed ${ghoul.t()} your blood, but nothing happens.");
            return false;
        }
        void ExpendBlood(EnthralledGhoul ghoul, bool showPopup)
        {

            ghoul.Object.GetPart<GhoulBloodMetabolism>().Buff(Roll());
            string basemessage = $"You feed {ghoul.Object.t()} your blood.";
            string output = showPopup == true ? $"{basemessage} and enthrall their mind." : $"{basemessage}.";
            base.ExpendBlood(showPopup, output);

        }
        static void RemoveLastGhoul(GameObject Object)
        {
            foreach (var obj in Object.Brain.PartyMembers.ToArray())
            {
                var ghoul = obj.Value.Reference?.Object;
                if (ghoul?.RemoveEffect<EnthralledGhoul>() ?? false)
                {
                    Object.Brain.PartyMembers.Remove(obj.Key);
                    return;
                }
            }
        }
        protected override void CollectStats(Templates.StatCollector stats)
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
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), VampirismSys.Rules.Ghoul.COOLDOWN);
        }
        public override void RemoveSpell()
        {
            RemoveLastGhoul(ParentObject);
            base.RemoveSpell();
        }
    }
}