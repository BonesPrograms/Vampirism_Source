using System;
using XRL.World.Effects;
using Nexus.Core;
using Nexus.Rules;
using XRL.World.AI;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{

    [Serializable]
    public class GhoulSpell : VampiricSpell
    {
        public Effect Ghoul;
        public override bool ShouldSync() => true;
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
            if (E.Means == "Ghoul" && E.Actor == ParentObject && ID != Guid.Empty)
            {
                cmd.msg($"First limt {E.Limit}");
                E.Limit = E.Limit + MAX();
                cmd.msg($"Limit {E.Limit} Max {MAX()}");
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            cmd.msg($"{ParentObject.Level}, {ParentObject.GetStat("Level").Value} level values");
            if (E.Command == GHOUL.COMMAND_NAME && Checks.Prerequisites(ParentObject, GHOUL.ABILITY_NAME, TEXT))
            {
                if (ParentObject.TryGetTarget(GHOUL.ABILITY_NAME, TEXT, out GameObject pick))
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

        public override void SyncLevels(int NewLevel)
        {
            CheckGhouls();
            foreach (var e in Ghouls.Values)
                e.SyncLevels(NewLevel);
            base.SyncLevels(NewLevel);
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
            if (base.Cast("Enthrall Ghoul", GHOUL.COOLDOWN, TEXT))
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
        Capabilities.Mental.PerformAttack(Enthrall, ParentObject, Target, null, GHOUL.COMMAND_NAME, "1d8", 1, int.MinValue, int.MinValue, Roll(), Target.Stat("Level"));

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
            foreach (var ghoul in Ghouls.KeyArray())
            {
                if ((!ghoul?.HasHitpoints() ?? true) || !ghoul.HasEffect(Ghouls[ghoul]))
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
            stats.CollectCooldownTurns(MyActivatedAbility(ID), GHOUL.COOLDOWN);
        }

        public override void RequireObject()
        {
            ID = AddMyActivatedAbility(GHOUL.ABILITY_NAME, GHOUL.COMMAND_NAME, TAG, null, "\u009f");
        }

        public override void RemoveObject()
        {
            CheckGhouls();
            foreach (var obj in Ghouls)
            {
                obj.Key.RemoveEffect(obj.Value);
            }
            SyncTarget(ParentObject);
            base.RemoveObject();
        }
        public static void SyncTarget(GameObject Beguiler, GameObject Target = null)
        {
            if (Beguiler.Brain == null)
            {
                return;
            }
            int num = GetCompanionLimitEvent.GetFor(Beguiler, "Ghoul");
            if (Target == null)
            {
                num++;
            }
            PartyCollection partyMembers = Beguiler.Brain.PartyMembers;
            int[] array = (from x in partyMembers
                           where x.Value.Flags.HasBit(2)
                           orderby Brain.PartyMemberOrder(x) descending
                           select x.Key).ToArray();
            int num2 = 0;
            for (int num3 = array.Length; num3 >= num; num3--)
            {
                partyMembers.Remove(array[num2]);
                num2++;
            }
            if (Target != null)
            {
                partyMembers[Target] = 2;
            }
        }
    }
}