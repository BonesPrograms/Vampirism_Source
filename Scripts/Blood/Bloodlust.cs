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
        public override string GetDetails() => Gameover ? "I need more." : Details(base.Object.GetStringProperty(Flags.BLOOD_STATUS));
        public override string GetDescription() => Gameover ? "{{R|bloodlusted}}" : Description(base.Object.GetStringProperty(Flags.BLOOD_STATUS));
        static string Description(string text) => text == Flags.Blood.MIN ? "{{R sequence|ravenous}}" : "{{R sequence|thirsty}}";
        static string Details(string text)
         =>
            text switch
            {
                Flags.Blood.THIRSTY => "{{R sequence|The Beast}} within howls to feed.",
                Flags.Blood.PARCHED => "If you do not sate {{R sequence|the Beast}}, it will sate itself.",
                Flags.Blood.MIN => "{{R sequence|The Beast}} is desperate. It will take control soon.",
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
            if (!base.WantEvent(ID, cascade))
                return ID == SingletonEvent<BeginTakeActionEvent>.ID;
            return true;
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            int vitae = base.Object.GetIntProperty(Flags.BLOOD_VALUE);
            if ((Duration = vitae >= VITAE.BLOOD_QUENCHED ? default : Duration) != 0)
                DiseaseStatus(vitae);
            return base.HandleEvent(E);
        }

        void DiseaseStatus(int vitae)
        {
            if (!Gameover)
            {
                switch (base.Object.GetStringProperty(Flags.BLOOD_STATUS))
                {
                    case Flags.Blood.PARCHED:
                        StageOne();
                        break;
                    case Flags.Blood.MIN:
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
            if (!Scan.ReturnProperty(base.Object, Flags.FRENZY))
                AddPlayerMessage(Gameover ? "You gorge on as much blood as you can, but your {{r|bloodlust}} will never truly be satiated." : "Your {{R sequence|thirst}} is quenched.");
            base.Object.GetPart<Vitae>().Bloodlusted = false;
        }

        public override bool SameAs(Effect e)
        {
            return false;
        }

        public override bool Apply(GameObject Object)
        {
            AutoAct.Interrupt();
            if (!Scan.ReturnProperty(Object, Flags.FRENZY))
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
