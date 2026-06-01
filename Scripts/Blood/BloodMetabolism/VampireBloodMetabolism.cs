using System;
using System.Collections.Generic;
using XRL.UI;
using VampirismSys.Properties;
using VampirismSys.Extensions;
using VampirismSys.Registry;
using VampirismSys.Blood;
using VampirismSys.Rules;
using XRL.World.Capabilities;
using XRL.World.Effects;


namespace XRL.World.Parts
{

    [Serializable]

    public class VampireBloodMetabolism : BaseBloodMetabolism
    {

        public bool GameOver { get => _gameOver; private set { _gameOver = value; } }
        bool _gameOver = false;
        bool Bloodlusted = false;
        protected override bool WantsMetabolism => ParentObject.IsPlayer(); //in the future, vampire AI may metabolize if theyre in the same party as player
        protected override bool WantsVomit => !ParentObject.CheckFlag(Flags.FRENZY);    //however, their metabrate will be at least 1/2 (similar to ghouls)
        public static bool AntiPuke;
        public int BloodDrams => ParentObject.GetFreeDrams("blood"); //for harmony
        public override string UIBloodDisplay => ParentObject.CheckFlag(Flags.GO) ? "{{r|Bottomless}}" : base.UIBloodDisplay;
        static readonly List<GameObject> containers = new();
        public VampireBloodMetabolism()
        {

        }

        public VampireBloodMetabolism(bool gameOver, bool bloodlusted, int blood)
        {
            this.GameOver = gameOver;
            this.Bloodlusted = bloodlusted;
            this.Blood = blood;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(Events.GAMEOVER);
            Registrar.Register(Events.WISH_HUMANITY);
            base.Register(Object, Registrar);
        }
        public override bool FireEvent(Event E)
        {
            if (ParentObject.IsPlayer())
            {
                if (E.ID == Events.WISH_HUMANITY)
                    GameOver = false;
                if (E.ID == Events.GAMEOVER)
                    GameOver = true;
            }
            return base.FireEvent(E);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == AfterPlayerBodyChangeEvent.ID)
                return true;
            if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
                return WantsAutoget();
            if (ID == EffectRemovedEvent.ID)
                return Bloodlusted;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect.GetType() == typeof(Bloodlust))
                Bloodlusted = false;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (AntiPuke && Blood >= VampirismSys.Rules.Metab.SIP_PUKE_WARN)
                Blood = 1;
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
        {
            Autoget.Clear();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            Autoget.Autogetter();
            return base.HandleEvent(E);
        }

        protected override void Cycle()
        {
            Bleeding();
            SetStomach();
            SetBloodProperties();
            base.Cycle();
            if (WantsAutosip())
                BloodAutoSip();
            CheckForBloodlust();
        }

        public bool PukeWarning(bool feeding)
        {
            if (!ParentObject.CheckFlag(Flags.FRENZY) && !ParentObject.Incap(false) && ParentObject.IsPlayer())
            {
                if (Blood >= VampirismSys.Rules.Metab.FEED_PUKE_WARN && feeding)
                {
                    if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                        return true;
                }
                else if (Blood >= VampirismSys.Rules.Metab.SIP_PUKE_WARN && !feeding)
                {
                    if (Popup.ShowYesNo("Drinking that much will probably make you sick. Do you still want a drink?") == DialogResult.No)
                        return true;
                }
            }
            return false;
        }

        void SetStomach()
        {
            if (Options.GetOptionBool(ModOptions.TRUE_UNDEAD) && Stomach != null && Stomach.HungerLevel != 0)   //most True Undead code is in Vampirism, this is the only one outside of it
                Stomach.ClearHunger();
        }

        void SetBloodProperties()
        {
            ParentObject.SetStringProperty(Flags.BLOOD_STATUS, StringStatus);
            ParentObject.SetIntProperty(Flags.BLOOD_VALUE, Blood);
        }

        void Bleeding()
        {
            if (ParentObject.HasEffect<Bleeding>() && Options.GetOptionBool(ModOptions.BLEED_THIRST))
            {
                Blood -= ParentObject.CheckFlag(Flags.FEED) ? VampirismSys.Rules.Metab.BLOOD_PERBloodLOSS_FEED : VampirismSys.Rules.Metab.BLOOD_PERBloodLOSS;
                IComponent<GameObject>.AddPlayerMessage("Bloodloss makes you {{R|thistier}}!");
            }
        }

        void CheckForBloodlust()
        {
            if (!Bloodlusted && Status < BloodLevel.QUENCHED)
            {
                Bloodlusted = true;
                ParentObject.ApplyEffect(new Bloodlust(9999, GameOver));
            }
        }

        void BloodAutoSip()
        {
            if (WantsAutosip(Options.GetOption(ModOptions.AUTOSIP_LEVEL)))
            {
                containers.Clear();
                if (ParentObject.UseDrams(1, "blood", null, null, null, containers, true))
                {
                    Drink();
                    Sip();
                }
                containers.Clear();
            }
        }

        void Sip()
        {
            GameObject gameObject = (containers.Count != 0) ? containers[0] : null;
            if (gameObject is null)
                DidX("take", "a sip of {{R sequence|blood}}", null, null, null, ParentObject);
            else
            {
                ParentObject.FireEvent(Event.New("DrinkingFrom", "Container", gameObject));
                DidXToY("take", "a sip of {{R sequence|blood}} from", gameObject, null, null, null, null, ParentObject, null, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: false, IndefiniteObjectForOthers: false, PossessiveObject: false, null, ParentObject);
            }
        }

        bool WantsAutosip()
         =>
            Options.GetOptionBool(ModOptions.AUTOSIP)
            && !Options.GetOptionBool(ModOptions.HUNTER)
            && !ParentObject.CheckFlag(Flags.FRENZY, Flags.FEED)
            && !ParentObject.Incap(false)
            && !ParentObject.IsPolymorphed();

        bool WantsAutosip(string option)
         => option switch
         {
             ModOptions.Autosip_Settings.QUENCH => Status < BloodLevel.GLUT, //in our code, being marked as "thirsty" actually means your blood is > thirsty and < quenched
             ModOptions.Autosip_Settings.THIRSTY => Status < BloodLevel.QUENCHED,//kind of confusing but i dont care to change it now
             ModOptions.Autosip_Settings.PARCHED => Status < BloodLevel.THIRSTY, //though with the new enum setup it is less confusing now
             ModOptions.Autosip_Settings.MIN => Status < BloodLevel.PARCHED,
             _ => false,
         };

        bool WantsAutoget()
         =>
            ParentObject.IsPlayer()
            && Options.GetOptionBool(ModOptions.AUTOGET)
            && !Options.GetOptionBool(ModOptions.HUNTER)
            && !AutoAct.IsResting()
            && !ParentObject.IsInCombat()
            && !ParentObject.CheckFlag(Flags.FRENZY, Flags.FEED)
            && !ParentObject.IsPolymorphed();
    }


    [Serializable]

    [Obsolete("Use VampireBloodMetabolism")]
    public class Vitae : IPart
    {
        public int Blood;
        public bool GameOver;
        public bool Bloodlusted;
        public override bool WantEvent(int ID, int Cascade)
        {
            return ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID;
        }

        public override bool HandleEvent(BeforeBeginTakeActionEvent E)
        {
            ParentObject.RemoveStringProperty("OldVampirismSaveNeedsUpdate");
            ParentObject.AddPart(new VampireBloodMetabolism(GameOver, Bloodlusted, Blood));
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
    }


}