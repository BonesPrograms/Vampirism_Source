using System;
using XRL.Core;
using XRL.World.Parts;
using XRL.World.Capabilities;
using Nexus.Properties;
using Nexus.Rules;
using Nexus.Core;
using Nexus.Registry;

namespace XRL.World.Effects
{
    /// <summary>
    /// Manages player feedback for their frenzy chance, based on thirst level, but does not determine Frenzy chance - see FrenzyCore.cs.
    /// </summary>
    [Serializable]
    public class Bloodlust : Effect
    {
        public bool stage1;
        public bool stage2;
        public bool Gameover;
        public Bloodlust() => DisplayName = "";
        public Bloodlust(int Duration, bool Gameover)
            : this()
        {
            base.Duration = Duration;
            this.Gameover = Gameover;
        }
        public override string GetDetails() => Gameover ? "I need more." : Details(base.Object.GetStringProperty(FLAGS.BLOOD_STATUS));
        public override string GetDescription() => Gameover ? "{{R|bloodlusted}}" : Description(base.Object.GetStringProperty(FLAGS.BLOOD_STATUS));
        static string Description(string text) => text == FLAGS.BLOOD.MIN ? "{{R sequence|ravenous}}" : "{{R sequence|thirsty}}";
        static string Details(string text)
         =>
            text switch
            {
                FLAGS.BLOOD.THIRSTY => "{{R sequence|The Beast}} within howls to feed.",
                FLAGS.BLOOD.PARCHED => "If you do not sate {{R sequence|the Beast}}, it will sate itself.",
                FLAGS.BLOOD.MIN => "{{R sequence|The Beast}} is desperate. It will take control soon.",
                "Error" => OutOfRange(),
                _ => "Loading... please pass turn.",
            };

        static string OutOfRange()
        {
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Bloodlust.Details() - Flags.BLOOD_STATUS returned Error, all values in BloodMetabolism.TurnBoolToString() returned false, should not be possible.");
            return "Error - see player log.";
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar) => Registrar.Register(Events.GAMEOVER); //incase you enter gameover while thirst is active
        public override bool FireEvent(Event E)
        {
            if (E.ID == Events.GAMEOVER)
                Gameover = true;
            return base.FireEvent(E);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            int vitae = base.Object.GetIntProperty(FLAGS.BLOOD_VALUE);
            if (vitae >= VITAE.BLOOD_QUENCHED)
                Duration = 0;
            else
                DiseaseStatus(vitae);
            return base.HandleEvent(E);
        }

        void DiseaseStatus(int vitae)
        {
            if (!Gameover)
            {
                switch (base.Object.GetStringProperty(FLAGS.BLOOD_STATUS))
                {
                    case FLAGS.BLOOD.PARCHED:
                        StageOne();
                        break;
                    case FLAGS.BLOOD.MIN:
                        StageTwo();
                        break;
                }
            }
            CheckBloodLevel(vitae);
        }

        void StageTwo()
        {
            if (!stage2 && stage1)
            {
                AutoAct.Interrupt();
                AddPlayerMessage("You feel {{r sequence|ravenous}}. It will be hard to control yourself.");
                stage2 = true;
            }
        }

        void StageOne()
        {
            if (!stage1)
            {
                AutoAct.Interrupt();
                AddPlayerMessage("Your {{R sequence|thirst}} grows stronger. It is time to hunt.");
                stage1 = true;
            }
        }
        void CheckBloodLevel(int vitae)
        {
            if (vitae > VITAE.BLOOD_PARCHED && stage2)
                stage2 = false;
            if (vitae > VITAE.BLOOD_THIRSTY && stage1)
                stage1 = false;
        }

        public override void Remove(GameObject Object)
        {
            if (!Object?.CheckFlag(FLAGS.FRENZY) ?? false && Object.IsPlayer())
                AddPlayerMessage(Gameover ? "You gorge on as much blood as you can, but your {{r|bloodlust}} will never truly be satiated." : "Your {{R sequence|thirst}} is quenched.");
            Vitae v = Object?.GetPart<Vitae>();
            v?.SetBloodlust(false);
        }

        public override bool SameAs(Effect e)
        {
            return false;
        }

        public override bool Apply(GameObject Object)
        {
            AutoAct.Interrupt();
            if (!Object?.CheckFlag(FLAGS.FRENZY) ?? false && Object.IsPlayer())
                AddPlayerMessage(Gameover ? "You are {{r|lusting}} for blood." : "You feel {{R sequence|thirsty}}.");
            return true;
        }
        public override bool Render(RenderEvent E)
        {
            if (Duration > 0 && XRLCore.CurrentFrame % 20 > 10)
            {
                E.RenderString = "\u0003";
                E.ColorString = "&r";
            }
            return true;
        }
    }
}
