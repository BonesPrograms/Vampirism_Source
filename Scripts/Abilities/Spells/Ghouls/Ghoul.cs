using XRL.World.Parts;
using XRL.World.AI;
using System;
using Nexus.Rules;
using Nexus.Spells;

namespace XRL.World.Effects
{


    ///SUPER IMPORTANT READ
    ///WANTEVENT NOTE: TORCH taught us a lot about adding actions. I think I could use addinventoryaction or - i think doug told me to use tradeactions.
    /// We should look into dromads and other traders with Scan wish, see if they have parts that show how to add trade actions
    /// no not trade actions, i want companion actions... i think beguiling/other party stuff does that then, well see

    [Serializable]
    public class EnthralledGhoul : IScribedEffect
    {
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
        public override string GetDescription()
        {
            return "{{K|ghoul}}";
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplyProselytize");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplyProselytize")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.FireEvent(E);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID || ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID || ID == ApplyEffectEvent.ID || ID == CanApplyEffectEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }


        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CanApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeBeginTakeActionEvent E)
        {
            if (!CompanionCore.IsSupported(Master, Object, 6))
                Duration = 0;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (WasFedOn)
                DelayRegen();
            else
                OriginalRegenTime = 0;
            if (BuffTime > 0)
                BuffTime--;
            else if (Buffed)
                Debuff();
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

        // public override bool HandleEvent(EffectRemovedEvent E)
        // {
        //     if (E.Effect is IFeeding feed && feed == CurrentFeed)
        //     {
        //         int bonus = Roll() * 100;
        //         int time = GHOUL.REGEN - bonus;
        //         RegenTime = time < GHOUL.MIN ? GHOUL.MIN : time;
        //         RegenTime = 500;
        //         OriginalRegenTime = RegenTime;
        //         CurrentFeed = null;
        //     }
        //     return base.HandleEvent(E);
        // }

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
            BuffTime = Ghoul.BUFFTIME;
            Buffed = true;
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
            CompanionCore.Ally<AllyEnthralledGhoul>(Object, Master, "Ghoul", $"You enthrall {Object.t()}'s mind.", 6);
            CompanionCore.AllyOpinion<OpinionEnthralledGhoul>(Object, Master);
            return true;
        }
        public override void Remove(GameObject Object)
        {
            CompanionCore.Dismiss<AllyEnthralledGhoul>(Master, Object, "You release " + Object.t() + "'s mind");
            CompanionCore.DismissOpinion<OpinionEnthralledGhoul>(Object, Master);
            CompanionCore.SyncTarget(Master, "Ghoul", 6);
            Master = null;
            base.Remove(Object);
        }



    }
}

namespace XRL.World.AI
{

    [Serializable]
    public class AllyEnthralledGhoul : AllyProselytize
    {
        public override string GetText(GameObject Actor)
        {
            return "I am a thrall to " + Name + ".";
        }
    }

    [Serializable]
    public class OpinionEnthralledGhoul : OpinionProselytize
    {
        public override string GetText(GameObject Actor)
        {
            return "Enthralled me.";
        }
    }

}