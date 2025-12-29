using System;
using System.Collections.Generic;
using XRL.UI;
using Nexus.Properties;
using Nexus.Core;
using Nexus.Registry;
using Nexus.Blood;
using Nexus.Rules;
using XRL.World.Capabilities;

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

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(Events.GAMEOVER);
            Registrar.Register(Events.WISH_HUMANITY);
            Registrar.Register("AfterDrank");
            Registrar.Register("AddWater");
        }
        public override bool FireEvent(Event E)
        {
            if (ParentObject.IsPlayer() && ParentObject.IsOriginalPlayerBody())
            {
                switch (E.ID)
                {
                    case Events.WISH_HUMANITY:
                        GameOver = false;
                        break;
                    case Events.GAMEOVER:
                        GameOver = E.ID is Events.GAMEOVER;
                        break;
                    case "AfterDrank":
                        Helpers.ResetWater(ref ParentObject.GetPart<Stomach>().Water);
                        break;
                    case "AddWater": //makes it so that you can get dehydrated
                        int obj = (int)E.GetParameter("Amount");
                        if (obj < 0)
                        {
                            Blood += obj;
                        }
                        break;
                }

            }
            return base.FireEvent(E);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade) && ID != PooledEvent<InduceVomitingEvent>.ID && ID != SingletonEvent<EndTurnEvent>.ID)
                return ID == SingletonEvent<BeginTakeActionEvent>.ID;
            return true;
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            if (!Options.GetOptionBool(OPTIONS.HUNTER) && Options.GetOptionBool(OPTIONS.AUTOGET) && ParentObject.IsPlayer() && !AutoAct.IsResting()&& !ParentObject.IsInCombat())
                new Autoget(ParentObject).Autogetter();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (ParentObject.IsOriginalPlayerBody() && ParentObject.IsPlayer())
            {
                new BloodMetabolism(this).BloodMetabolismCycle();
                if (!Options.GetOptionBool(OPTIONS.HUNTER) && !Scan.SafeReturnProperty(ParentObject, Flags.FRENZY, Flags.FEED) && !Scan.Incap(ParentObject, false))
                    BloodAutoSip();
            }

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(InduceVomitingEvent E)
        {
            if (E.Object == ParentObject)
            {
                Helpers.ResetWater(ref E.Object.GetPart<Stomach>().Water);
                Helpers.VomitEventHandler(E.Object, E.MessageHolder);
                if (E.Object.IsPlayer() && E.Object.IsOriginalPlayerBody())
                    Blood -= WikiRng.Next(15000, 25000);
                E.InterfaceExit = true;
            }
            return base.HandleEvent(E);
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
                    Drink(false);
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
        public string BloodStatus() => Scan.SafeReturnProperty(ParentObject, Flags.GO) ? "{{r|Bottomless}}" : Switch();
        string Switch() =>
        Blood switch
        {
            >= VITAE.BLOOD_GLUTTONOUS => "{{G|Glutted}}",
            >= VITAE.BLOOD_QUENCHED and < VITAE.BLOOD_GLUTTONOUS => "{{g|Gorged}}",
            >= VITAE.BLOOD_THIRSTY and < VITAE.BLOOD_QUENCHED => "{{R|Thirsty}}",
            >= VITAE.BLOOD_PARCHED and < VITAE.BLOOD_THIRSTY => "{{r|Fiending}}",
            >= VITAE.BLOOD_MIN and < VITAE.BLOOD_PARCHED or < VITAE.BLOOD_MIN => "{{r|Ravenous}}"
        };
        
        public bool IDontWantToPuke(bool feeding) // didnt know what to name this one
        { //cannot know at compile time if you might be frenzying at any given moment
            if (!Scan.SafeReturnProperty(ParentObject, Flags.FRENZY) && !Scan.Incap(ParentObject, false) && ParentObject.IsPlayer())
            {
                if (Blood >= VITAE.FEED_PUKE_WARN && feeding)
                {
                    if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                        return true;
                }
                else if (Blood >= VITAE.SIP_PUKE_WARN && !feeding)
                {
                    if (Popup.ShowYesNo("Drinking that much will probably make you sick. Do you still want a drink?") == DialogResult.No)
                        return true;
                }
            }
            return false;
        }

        public void Drink(bool feeding)
        {
            Blood += feeding ? VITAE.BLOOD_PER_FEED : VITAE.BLOOD_PER_SIP;
            Event E = Event.New("AddFood");
            E.SetParameter("Satiation", "Snack");
            E.SetFlag("Meat", true);
            ParentObject.FireEvent(E);
            ParentObject.FireEvent(Event.New("AfterDrank")); //for glotrot. all you need is this event and glotrot seems to work intrinsically with putrefying blood
        }
    }
}