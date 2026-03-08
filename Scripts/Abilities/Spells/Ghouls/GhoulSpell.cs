using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using Nexus.Spells;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{

    [Serializable]
    public class GhoulSpell : VampiricSpell
    {

        public override string CommandName => Nexus.Rules.Ghoul.COMMAND_NAME;
        public override string AbilityMenuName => Nexus.Rules.Ghoul.ABILITY_NAME;
        public override int Cooldown => Nexus.Rules.Ghoul.COOLDOWN;
        public Effect Ghoul;
        public Dictionary<GameObject, EnthralledGhoul> Ghouls = new();
        int MAX()
            => Level switch
            {
                <= 5 => 2,
                <= 10 => 2,
                <= 15 => 3,
                <= 20 => 4,
                <= 25 => 5,
                > 25 => 5
            };
        const string TEXT = "to enthrall";
        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == PooledEvent<GetCompanionLimitEvent>.ID || ID == PooledEvent<GetCompanionStatusEvent>.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(GetCompanionStatusEvent E)
        {
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetCompanionLimitEvent E)
        {
            if (E.Means == "Ghoul" && E.Actor == ParentObject && SpellID != Guid.Empty)
            {
               // admn.msg($"First limt {E.Limit}");
                E.Limit = E.Limit + MAX();
               // admn.msg($"Limit {E.Limit} Max {MAX()}");
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
           // admn.msg($"{ParentObject.Level}, {ParentObject.GetStat("Level").Value} level values");
            if (E.Command == Nexus.Rules.Ghoul.COMMAND_NAME && Checks.Prerequisites(base.ParentObject, Nexus.Rules.Ghoul.ABILITY_NAME, TEXT))
            {
                if (base.ParentObject.TryGetTarget(Nexus.Rules.Ghoul.ABILITY_NAME, TEXT, out GameObject pick))
                {
                    if (Checks.Attackable(pick, TEXT))
                    {
                        CheckGhouls();
                        MakeAttack(pick);
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public void MakeAttack(GameObject Target)
        {
            if (!ParentObject.IsRealityDistortionUsable())
                RealityStabilized.ShowGenericInterdictMessage(ParentObject);
            else if (!AlreadyEnthralled(Target, out bool containskey) && !IsVampire(Target))
                Cast(Target, containskey);
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

        bool AlreadyEnthralled(GameObject Target, out bool containskey)
        {
            containskey = Ghouls.ContainsKey(Target);
            if (!containskey && Target.HasEffect<EnthralledGhoul>())
            {
                UI.Popup.Show($"{Target.t()} is already enthralled by someone else.");
                return true;
            }
            return false;
        }

        public void ExpendBlood(GameObject Target, bool iskey)
        {
            Ghouls[Target].Buff(Roll());
            base.ExpendBlood(iskey, $"You feed {Target.t()} your blood.");
        }


        void Cast(GameObject Target, bool containskey)
        {
            if (base.Cast(TEXT))
            {
                if (containskey)
                    this.ExpendBlood(Target, true);
                else if (Prerequisites(Target) && Attack(Target))
                {
                    //  if (Ghouls.Count == MAX())
                    //  {
                    //      Ghouls.ElementAt(0).Key.RemoveEffect<EnthralledGhoul>();
                    //  }
                    ApplyGhoulEffect(Target);
                }
            }
        }

        void ApplyGhoulEffect(GameObject Target)
        {
            EnthralledGhoul ghoul = new(ParentObject);
            if (Target.ApplyEffect(ghoul))
            {
                Ghouls.Add(Target, ghoul);
                this.ExpendBlood(Target, false);
            }
        }

        bool Attack(GameObject Target) =>
        Capabilities.Mental.PerformAttack(Enthrall, base.ParentObject, Target, null, Nexus.Rules.Ghoul.COMMAND_NAME, "1d8", 1, int.MinValue, int.MinValue, base.Roll(), Target.Stat("Level"));

        public bool Prerequisites(GameObject Target)
        {
            if (!Target.FireEvent("CanApplyBeguile") || !CanApplyEffectEvent.Check(Target, "Beguile"))
            {
                IComponent<GameObject>.AddPlayerMessage(Target.Does("seem") + " utterly impervious to your charms.");
                return false;
            }
            return base.RealityCheck(Target.CurrentCell);
        }

        void CheckGhouls()
        {
            foreach (var ghoul in Ghouls.Keys.ToArray())
            {
                if ((!ghoul?.HasHitpoints() ?? true) || !ghoul.HasEffect<EnthralledGhoul>())
                    Ghouls.Remove(ghoul);
            }
        }
        bool Enthrall(MentalAttackEvent E)
        {
            GameObject defender = E.Defender;
            if (E.Penetrations <= 0 || !defender.FireEvent("CanApplyBeguile") || !CanApplyEffectEvent.Check(defender, "Beguile"))
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
            CheckGhouls();
            foreach (var obj in Ghouls)
            {
                obj.Key.RemoveEffect(obj.Value);
            }
            MasterCore.SyncTarget(ParentObject, "Ghoul", 6);
            base.RemoveSpell();
        }
    }
}