using System;
using System.Collections.Generic;
using XRL.UI;
using Nexus.Properties;
using Nexus.Core;
using Nexus.Registry;
using Nexus.Blood;
using Nexus.Rules;
using XRL.World.Capabilities;
using Nexus.Patches;

namespace XRL.World.Parts
{

    /// <summary>
    /// The blood-based, liquid-only Stomach part, which overrides water and manages all features related to blood.
    /// </summary>
    [Serializable]

    public class Vitae : IPart //vitae is stricter than humanity - i did not intend for, or test, AI metabolism
    {                           //even if dominating a vampire, their blood value will never change. might change this one day

        [NonSerialized]
        public static List<GameObject> containers = new();
        public int BloodDrams => ParentObject.GetFreeDrams("blood"); //for harmony
        public int Blood = VITAE.BLOOD_GLUTTONOUS;
        public bool GameOver;
        public bool Bloodlusted;

        [NonSerialized]
        public bool AntiPuke;
        BloodMetabolism _Metab;
        public BloodMetabolism Metab => _Metab ??= new BloodMetabolism(this);
        Autoget _Autoget;
        public Autoget Autoget => _Autoget ??= new Autoget(ParentObject);

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(Events.GAMEOVER);
            Registrar.Register(Events.WISH_HUMANITY);
            Registrar.Register("AfterDrank");
            Registrar.Register("AddWater");
        }
        public override bool FireEvent(Event E)
        {
            if (ParentObject.IsPlayer())
            {
                switch (E.ID)
                {
                    case Events.WISH_HUMANITY:
                        GameOver = false;
                        break;
                    case Events.GAMEOVER:
                        GameOver = true;
                        break;
                    case "AfterDrank":
                        Overrides.Water(ref ParentObject.GetPart<Stomach>().Water);
                        break;
                    case "AddWater": //makes it so that you can get dehydrated
                        object obj = E.GetParameter("Amount");
                        if (obj is int integer && integer < 0)
                            Blood += integer;
                        break;
                }

            }
            return base.FireEvent(E);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == PooledEvent<InduceVomitingEvent>.ID)
                return true;
            if (ParentObject.IsPlayer())
            {
                if (ID == SingletonEvent<BeforeTakeActionEvent>.ID && !AutoAct.IsResting() && !ParentObject.Incap(false) && !ParentObject.IsInCombat() && Options.GetOptionBool(OPTIONS.AUTOGET) && !Options.GetOptionBool(OPTIONS.HUNTER) && !ParentObject.CheckFlag(FLAGS.FRENZY, FLAGS.FEED))
                    return true;
                if (ID == SingletonEvent<BeginTakeActionEvent>.ID)
                    return true;
            }
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            Autoget.Autogetter();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (AntiPuke && Blood >= VITAE.SIP_PUKE_WARN || Blood >= VITAE.FEED_PUKE_WARN || Blood >= VITAE.GHOUL_PUKE_WARN)
                Blood = 1;
            Metab.Cycle();
            if (!Options.GetOptionBool(OPTIONS.HUNTER) && !ParentObject.CheckFlag(FLAGS.FRENZY, FLAGS.FEED) && !ParentObject.Incap(false))
                BloodAutoSip();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(InduceVomitingEvent E)
        {
            if (E.Object == ParentObject)
            {
                Overrides.Water(ref E.Object.GetPart<Stomach>().Water);
                Overrides.VomitEventHandler(E.Object, E.MessageHolder);
                if (E.Object.IsPlayer())
                    Blood -= WikiRng.Next(15000, 25000);
                E.InterfaceExit = true;
                return false;
            }
            return base.HandleEvent(E);
        }

        public void SubtractBlood(int value)
        {
            Blood -= value;
        }

        public void AddBlood(int value)
        {
            Blood += value;
        }

        public void SetBloodlust(bool value)
        {
            Bloodlusted = value;
        }

        public void SetBlood(int value)
        {
            Blood = value;
        }

        bool ItsTimeToDrink(string option)
        {
            switch (option)
            {
                case OPTIONS.AUTOSIP_LEVELS.GLUT:
                    if (Blood < VITAE.BLOOD_GLUTTONOUS)
                        return true;
                    break;
                case OPTIONS.AUTOSIP_LEVELS.QUENCH:
                    if (Blood < VITAE.BLOOD_QUENCHED)
                        return true;
                    break;
                case OPTIONS.AUTOSIP_LEVELS.THIRSTY:
                    if (Blood < VITAE.BLOOD_THIRSTY)
                        return true;
                    break;
                case OPTIONS.AUTOSIP_LEVELS.PARCHED:
                    if (Blood < VITAE.BLOOD_PARCHED)
                        return true;
                    break;
                case OPTIONS.AUTOSIP_LEVELS.MIN:
                    if (Blood <= VITAE.BLOOD_MIN)
                        return true;
                    break;
            }
            return false;
        }

        void BloodAutoSip()
        {
            if (Options.GetOptionBool(OPTIONS.AUTOSIP) && ItsTimeToDrink(Options.GetOption(OPTIONS.AUTOSIP_LEVEL)))
            {
                containers.Clear();
                if (ParentObject.UseDrams(1, "blood", null, null, null, containers, true))
                {
                    Drink(false, false);
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
        public string BloodStatus() => ParentObject.CheckFlag(FLAGS.GO) ? "{{r|Bottomless}}" : Switch();
        string Switch() =>
        Blood switch
        {
            >= VITAE.BLOOD_GLUTTONOUS => "{{G|Glutted}}",
            >= VITAE.BLOOD_QUENCHED and < VITAE.BLOOD_GLUTTONOUS => "{{g|Gorged}}",
            >= VITAE.BLOOD_THIRSTY and < VITAE.BLOOD_QUENCHED => "{{R|Thirsty}}",
            >= VITAE.BLOOD_PARCHED and < VITAE.BLOOD_THIRSTY => "{{r|Fiending}}",
            >= VITAE.BLOOD_MIN and < VITAE.BLOOD_PARCHED or < VITAE.BLOOD_MIN => "{{r|Ravenous}}"
        };

        public bool IDontWantToPuke(bool feeding, bool ghoul) // didnt know what to name this one
        { //cannot know at compile time if you might be frenzying at any given moment
            if (!ParentObject.CheckFlag(FLAGS.FRENZY) && !ParentObject.Incap(false) && ParentObject.IsPlayer())
            {
                // if (ghoul && Blood >= VITAE.GHOUL_PUKE_WARN)
                // {
                //     if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                //         return true;
                // }
                if (Blood >= VITAE.FEED_PUKE_WARN && feeding)
                {
                    if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                        return true;
                }
                else if (Blood >= VITAE.SIP_PUKE_WARN && !feeding && !ghoul)
                {
                    if (Popup.ShowYesNo("Drinking that much will probably make you sick. Do you still want a drink?") == DialogResult.No)
                        return true;
                }
            }
            return false;
        }

        public void Drink(bool feeding, bool ghoul)
        {
            //  if (ghoul)
            //      Blood += VITAE.BLOOD_PER_GHOUL;
            Blood += feeding ? VITAE.BLOOD_PER_FEED : VITAE.BLOOD_PER_SIP;
            Event E = Event.New("AddFood");
            E.SetParameter("Satiation", "Snack");
            E.SetFlag("Meat", true);
            ParentObject.FireEvent(E);
            ParentObject.FireEvent(Event.New("AfterDrank")); //for glotrot. all you need is this event and glotrot seems to work intrinsically with putrefying blood
        }
    }
}