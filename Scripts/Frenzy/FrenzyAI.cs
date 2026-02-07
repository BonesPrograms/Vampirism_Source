using XRL.Core;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Capabilities;
using Nexus.Properties;
using Nexus.Core;

using Nexus.Rules;
using Nexus.Frenzy;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
    /// <summary>
    /// The pseudo-AI that paths to and attacks the target assigned to it by Frenzy().
    /// </summary>
    public class FrenzyAI : Effect
    {
        public GameObject Target;
        public readonly TheBeast Source;
        readonly Action Action;
        public bool InRange => base.Object.DistanceTo(Target) <= 1;
        public readonly bool gameover;
        bool activated;
        int feedtime;

        public FrenzyAI() => DisplayName = "frenzyAI";
        public FrenzyAI(int Duration, TheBeast Source, GameObject Target, bool gameover)
            : this()
        {
            base.Duration = Duration;
            this.Target = Target;
            this.gameover = gameover;
            this.Source = Source;
            Action = new(this, Source.Base.FeedCommand.Bite, Source.Core.Search);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (!base.WantEvent(ID, Cascade) && ID != SingletonEvent<BeginTakeActionEvent>.ID && ID != TookDamageEvent.ID)
                return ID == SingletonEvent<EndTurnEvent>.ID;
            return true;
        }
        public override bool HandleEvent(TookDamageEvent E)
        {
            if (ValidTarget(E.Actor, Object) && !BadKey(E.Actor))
                Target = E.Actor;
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (Duration > 0)
                base.Object.PassTurn(); //must be in BTA event or will cause "ghost turns" to process after effect ends
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            XRLCore.Core.RenderDelay(100);
            EndingTimer();
            Action.Act();
            return base.HandleEvent(E);
        }

        bool ValidTarget(GameObject Actor, GameObject Object)
        =>
            Object == base.Object
            && !InRange
            && !Object.CheckFlag(FLAGS.FEED) //fun bug here. because frenzy never uses energy, if you are attacked by a group, you will stack feeding on all of them and become god. so we check for if FEED == false before swapping targets
            && Actor?.CurrentCell?.GetCombatTarget(base.Object) != null
            && Checks.Applicable(Actor)
            && base.Object.HasLOSTo(Actor, IncludeSolid: false)
            && base.Object.canPathTo(Actor.CurrentCell)
            && Actor.IsVisible()
            && !Actor.IsFlying;


        /// <summary>
        /// Prevents already-avoided objects from being assigned to Target to prevent softlocking.
        /// </summary>
        bool BadKey(GameObject Actor)
        {
            Source.TargetRegistry.TryGetValue(Actor, out int value);
            return value == TheBeast.FLAG_AVOID;
        }

        /// <summary>
        /// Initiates a local timer for feeding duration to avoid desync and ensure Frenzy is removed when feed is over.
        /// </summary>
        void EndingTimer()
        {
            if (!gameover)
            {
                if (!activated && base.Object.CheckFlag(FLAGS.FEED))
                    StartCountdown();
                if (activated)
                    Countdown();
            }
        }

        void Countdown()
        {
            feedtime--;
            if (feedtime == 0 || !base.Object.CheckFlag(FLAGS.FEED))
                Duration = 0;
        }

        void StartCountdown()
        {
            activated = true;
            feedtime = FEED.DURATION;
        }

        public override void Remove(GameObject Object)
        {
            AutoAct.Interrupt();
            XRLCore.Core.RenderDelay(100);
            if (gameover == false) //prevents msg spam since you constantly frenzy
                Popup.Show("{{R sequence|The Beast}} releases you.");
            Cleanup();
        }

        void Cleanup()
        {
            CheckBloodAndCooldown();
            Source.frenzied = false;
            base.Object.SetStringProperty(FLAGS.FRENZY, FLAGS.FALSE);
        }

        void CheckBloodAndCooldown()
        {
            Vitae vitae = base.Object.GetPart<Vitae>();
            Source.Base.CooldownMyActivatedAbility(Source.Base.FangsActivatedAbilityID, FEED.COOLDOWN);
            if (vitae.Blood >= VITAE.BLOOD_PUKE) //prevents vomit softlock from having 184,000 blood after a crazy wassail sesh
                vitae.Blood = VITAE.BLOOD_PUKE;
        }

        public override bool Apply(GameObject Object)
        {

            AutoAct.Interrupt(); //prevents graphics bugs that occur if frenzy activates while waiting
            XRLCore.Core.RenderDelay(100);
            base.Object.PassTurn(); // need to pass turn on apply or else you get a turn to act
            return true;
        }

        public override bool Render(RenderEvent E)
        {

            int num = XRLCore.CurrentFrame % 60;
            if (num > 25 && num < 35)
            {
                E.Tile = null;
                E.RenderString = "\u0003";
                E.ColorString = "&R^k";
            }
            return true;
        }

        public override bool SameAs(Effect e) => false;
        public override string GetDetails() => "{{R sequence|The Beast}} has taken control.";

    }
}
