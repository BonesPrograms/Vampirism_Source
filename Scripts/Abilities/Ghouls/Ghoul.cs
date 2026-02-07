using XRL.World.Parts;
using XRL.World.AI;
using System;
using Nexus.Rules;
using Nexus.Powers;

namespace XRL.World.Effects
{

            
///SUPER IMPORTANT READ
        ///WANTEVENT NOTE: TORCH taught us a lot about adding actions. I think I could use addinventoryaction or - i think doug told me to use tradeactions.
        /// We should look into dromads and other traders with Scan wish, see if they have parts that show how to add trade actions
        /// no not trade actions, i want companion actions... i think beguiling/other party stuff does that then, well see

    [Serializable]
    public class EnthralledGhoul : SpellEffect
    {
        public override bool ShouldSync() => true;
        public GameObject Master;
        public Effect CurrentFeed;
        public int CurrentRegen;
        public int RegenTime;
        public int OriginalRegenTime;
        public int BuffTime;
        public bool Buffed;
        public bool WasFedOn => RegenTime > 0;
        public EnthralledGhoul() => DisplayName = "{{K|ghoul}}";
        public EnthralledGhoul(GameObject Master) : this()
        {
            this.Master = Master;
            base.Duration = 9999;
        }
        public override int Roll() => IVampiricSpell.Roll(Master, Level);
        public override string GetDescription()
        {
            return "{{K|ghoul}}";
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (!IsSupported())
                Duration = 0;
            else
            {
                if (WasFedOn)
                    DelayRegen();
                else
                    OriginalRegenTime = 0;
                if (BuffTime > 0)
                    BuffTime--;
                else if (Buffed)
                    Debuff();
            }
            return base.HandleEvent(E);
        }

        public bool IsGhoulOf(GameObject Target)
        {
            return Target == Master;
        }

        void Debuff()
        {
            StatShifter.RemoveStatShift(Object, "Hitpoints");
            Buffed = false;
        }
        void DelayRegen()
        {
            CurrentRegen++;
            RegenTime--;
            int percent = CurrentRegen / OriginalRegenTime * 100;
            int newhp = percent / 100 * base.Object.baseHitpoints;
            newhp = newhp <= 0 ? 1 : newhp;
            base.Object.hitpoints = newhp;
            AddPlayerMessage($"{Object.hitpoints}, {newhp}, {percent}");

        }

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            if (E.Effect is IFeeding feed)
            {
                if (!feed.isAttacker && feed.other.Object == Master && feed.Object == Object)
                    CurrentFeed = feed;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect is IFeeding feed && feed == CurrentFeed)
            {
                int bonus = Roll() * 100;
                int time = GHOUL.REGEN - bonus;
                RegenTime = time < GHOUL.MIN ? GHOUL.MIN : time;
                RegenTime = 500;
                OriginalRegenTime = RegenTime;
                CurrentFeed = null;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(DeathEvent E)
        {
            GhoulSpell spell = Master?.GetPart<GhoulSpell>();
            spell?.Ghouls?.Remove(Object);
            return base.HandleEvent(E);
        }

        public void Buff(int Roll)
        {
            StatShifter.SetStatShift("Hitpoints", Roll); //"Hitpoints"
            Object.Heal(Roll);
            BuffTime = GHOUL.BUFFTIME;
            Buffed = true;
        }

        public bool IsSupported()
        {
            if (GameObject.Validate(ref Master) || !Master.HasHitpoints())
                return Master.SupportsFollower(base.Object, 2);
            return false;
        }


        public override bool Apply(GameObject Object)
        {
            if (!GameObject.Validate(ref Master))
                return false;
            if (Object.Brain == null)
                return false;
            if (!Object.FireEvent("CanApplyBeguile"))
                return false;
            if (!Object.FireEvent("ApplyBeguile"))
                return false;
            if (!ApplyEffectEvent.Check(Object, "Beguile", this))
                return false;
            Object.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_charm");
            AddPlayerMessage($"You enthrall {Object.t()}'s mind.");
            Object.Heartspray();
            GhoulSpell.SyncTarget(Master, Object);
            Object.SetAlliedLeader<AllyBeguile>(Master);
            Enthrall();
            return true;
        }
        public override void Remove(GameObject Object)
        {
            if (GameObject.Validate(ref Master) && Object.PartyLeader == Master && !Master.SupportsFollower(Object, 13))
            {
                Object.Brain.PartyLeader = null;
                Object.Brain.Goals.Clear();
                if (Object.InSameZone(Master?.CurrentCell))
                    AddPlayerMessage("{{R|You free}}" + Object.t() + "'s mind");
            }
            Object.Brain.RemoveAllegiance<AllyBeguile>(Master?.BaseID ?? 0);
            Free();
            Master = null;
            base.Remove(Object);
        }
        void Free()
        {
            GhoulSpell spell = Master.GetPart<GhoulSpell>();
            spell.Ghouls.Remove(Object);
            if (base.Object.Brain != null && GameObject.Validate(ref Master))
                base.Object.Brain.RemoveOpinion<OpinionBeguile>(Master);
        }

        void Enthrall()
        {
            if (base.Object.Brain != null && GameObject.Validate(ref Master))
                base.Object.Brain.AddOpinion<OpinionBeguile>(Master);
        }
    }
}