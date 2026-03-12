using XRL.Core;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Capabilities;
using VampirismSys.Properties;
using VampirismSys.Core;
using VampirismSys.Rules;
using VampirismSys.Frenzy;

namespace XRL.World.Effects
{
    /// <summary>
    /// The pseudo-AI that paths to and attacks the target assigned to it by Frenzy().
    /// </summary>
    public class FrenzyAI : Effect //this effect should NOT be applied directly and instead should be applied via FrenzyCore.Frenzy()
    {
        public GameObject Target;
        public readonly TheBeast Source;
        readonly Action Action;
        public bool InRange => base.Object.DistanceTo(Target) <= 1;
        public readonly bool gameover;
        internal FrenzyAI() => DisplayName = "";
        internal FrenzyAI(TheBeast Source, GameObject Target, bool gameover) : this()
        {
            base.Duration = 9999;
            this.Target = Target;
            this.gameover = gameover;
            this.Source = Source;
            Action = new(this, Source.Base.FeedAbility.Bite, Source.Core.Search);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == TookDamageEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID || ID == EffectRemovedEvent.ID || ID == KilledEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(KilledEvent E)
        {
            if (E.Killer == Object && E.Dying == Target)
            {
                if (!gameover)
                    Duration = 0;
                else
                    Target = null;
                //    Source.TargetRegistry.Remove(E.Dying); //Sift() will remove the target on its own
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (!gameover && E.Effect is IFeeding feed && feed.isAttacker)
            {
                Duration = 0;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TookDamageEvent E)
        {
            if (ValidTarget(E.Actor, E.Object) && !Source.Core.Search.BadKey(E.Actor))
            {
                Target = E.Actor;
            }
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
            if (Duration > 0)
            {
                XRLCore.Core.RenderDelay(100);
                Action.Act();
            }
            return base.HandleEvent(E);
        }

        bool ValidTarget(GameObject Actor, GameObject Object)
        =>
            Object == base.Object
            && !InRange
            && !Object.CheckFlag(Flags.FEED) //fun bug here. because frenzy never uses energy, if you are attacked by a group, you will stack feeding on all of them and become god. so we check for if FEED == false before swapping targets
            && this.Source.Core.Search.ValidForRegistration(Actor);


        public override void Remove(GameObject Object)
        {
            AutoAct.Interrupt();
            XRLCore.Core.RenderDelay(100);
            if (gameover == false && Object.IsPlayer()) //prevents msg spam since you constantly frenzy
                Popup.Show("{{R sequence|The Beast}} releases you.");
            Cleanup();
        }

        void Cleanup()
        {
            base.Object.RemoveEffect<Running>();
            CheckBloodAndCooldown();
            Source.Frenzied = false;
            Source.ParentObject.SetStringProperty(Flags.FRENZY, Flags.FALSE);
        }

        void CheckBloodAndCooldown()
        {
            Parts.VampireBloodMetabolism vitae = base.Object.GetPart<Parts.VampireBloodMetabolism>();
            Source.Base.CooldownMyActivatedAbility(Source.Base.FangsActivatedAbilityID, Feed.COOLDOWN);
            if (vitae.Blood >= VampirismSys.Rules.Vitae.BLOOD_PUKE) //prevents vomit softlock from having 184,000 blood after a crazy wassail sesh
                vitae.Blood = VampirismSys.Rules.Vitae.BLOOD_PUKE;
        }

        public override bool Apply(GameObject Object)
        {

            Source.ParentObject.SetStringProperty(Flags.FRENZY, Flags.TRUE);
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
