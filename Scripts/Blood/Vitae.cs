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

    public class Vitae : IPart
    {

        [NonSerialized]
        public static List<GameObject> containers = new();
        public int BloodDrams => ParentObject.GetFreeDrams("blood"); //for harmony
        public int Blood = Nexus.Rules.Vitae.BLOOD_GLUTTONOUS;
        public bool GameOver;
        public bool Bloodlusted;
        public static bool AntiPuke;
        BloodMetabolism _Metab; //cant really add new fields (vitae has already been serialized in many peoples saves) so i have not bothered to make this into a serializable object
        public BloodMetabolism Metab => _Metab ??= new(this);

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
                        if (obj is int integer && integer < 0 && ParentObject.IsPlayer())
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
            if (ID == AfterPlayerBodyChangeEvent.ID)
                return true;
            if (ID == SingletonEvent<BeforeTakeActionEvent>.ID && ParentObject.IsPlayer() && !AutoAct.IsResting() && !ParentObject.Incap(false) && !ParentObject.IsInCombat() && Options.GetOptionBool(ModOptions.AUTOGET) && !Options.GetOptionBool(ModOptions.HUNTER) && !ParentObject.CheckFlag(Flags.FRENZY, Flags.FEED) && !ParentObject.IsInBatForm())
                return true;
            if (ID == SingletonEvent<BeginTakeActionEvent>.ID && ParentObject.IsPlayer())
                return true;
            return base.WantEvent(ID, cascade);
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
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (AntiPuke && (Blood >= Nexus.Rules.Vitae.SIP_PUKE_WARN || Blood >= Nexus.Rules.Vitae.FEED_PUKE_WARN || Blood >= Nexus.Rules.Vitae.GHOUL_PUKE_WARN))
                Blood = 1;
            Metab.Cycle();
            if (!Options.GetOptionBool(ModOptions.HUNTER) && !ParentObject.CheckFlag(Flags.FRENZY, Flags.FEED) && !ParentObject.Incap(false) && !ParentObject.IsInBatForm())
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
            }
            return base.HandleEvent(E);
        }
        bool ItsTimeToDrink(string option)
        {
            switch (option)
            {
                case ModOptions.Autosip_Settings.QUENCH:
                    return Blood < Nexus.Rules.Vitae.BLOOD_GLUTTONOUS;
                case ModOptions.Autosip_Settings.THIRSTY: //in our code, being marked as "thirsty" actually means your blood is > thirsty and < quenched
                    return Blood < Nexus.Rules.Vitae.BLOOD_QUENCHED;//kind of confusing but i dont care to change it now
                case ModOptions.Autosip_Settings.PARCHED:
                    return Blood < Nexus.Rules.Vitae.BLOOD_THIRSTY;
                case ModOptions.Autosip_Settings.MIN:
                    return Blood < Nexus.Rules.Vitae.BLOOD_PARCHED;
            }
            return false;
        }

        void BloodAutoSip()
        {
            if (Options.GetOptionBool(ModOptions.AUTOSIP) && ItsTimeToDrink(Options.GetOption(ModOptions.AUTOSIP_LEVEL)))
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
        //im not really sure how containers is able to get your containers
        //outside of these methods, the list always seems to be empty, and i havent bothered debugging within the methods
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
        public string BloodStatus() => ParentObject.CheckFlag(Flags.GO) ? "{{r|Bottomless}}" : StatusToString();
        string StatusToString() =>
        Blood switch
        {
            >= Nexus.Rules.Vitae.BLOOD_GLUTTONOUS => "{{G|Glutted}}",
            >= Nexus.Rules.Vitae.BLOOD_QUENCHED and < Nexus.Rules.Vitae.BLOOD_GLUTTONOUS => "{{g|Gorged}}",
            >= Nexus.Rules.Vitae.BLOOD_THIRSTY and < Nexus.Rules.Vitae.BLOOD_QUENCHED => "{{R|Thirsty}}",
            >= Nexus.Rules.Vitae.BLOOD_PARCHED and < Nexus.Rules.Vitae.BLOOD_THIRSTY => "{{r|Fiending}}",
            >= Nexus.Rules.Vitae.BLOOD_MIN and < Nexus.Rules.Vitae.BLOOD_PARCHED or < Nexus.Rules.Vitae.BLOOD_MIN => "{{r|Ravenous}}"
        };

        public bool IDontWantToPuke(bool feeding) // didnt know what to name this one
        { //cannot know at compile time if you might be frenzying at any given moment
            if (!ParentObject.CheckFlag(Flags.FRENZY) && !ParentObject.Incap(false) && ParentObject.IsPlayer())
            {
                // if (ghoul && Blood >= VITAE.GHOUL_PUKE_WARN)
                // {
                //     if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                //         return true;
                // }
                if (Blood >= Nexus.Rules.Vitae.FEED_PUKE_WARN && feeding)
                {
                    if (Popup.ShowYesNo("Feeding that much will probably make you sick. Do you still want to feed?") == DialogResult.No)
                        return true;
                }
                else if (Blood >= Nexus.Rules.Vitae.SIP_PUKE_WARN && !feeding)
                {
                    if (Popup.ShowYesNo("Drinking that much will probably make you sick. Do you still want a drink?") == DialogResult.No)
                        return true;
                }
            }
            return false;
        }

        public void Drink(bool feeding)
        {
            //  if (ghoul)
            //      Blood += VITAE.BLOOD_PER_GHOUL;
            Blood += feeding ? Nexus.Rules.Vitae.BLOOD_PER_FEED : Nexus.Rules.Vitae.BLOOD_PER_SIP;
            Event E = Event.New("AddFood");
            E.SetParameter("Satiation", "Snack");
            E.SetFlag("Meat", true);
            ParentObject.FireEvent(E);
            ParentObject.FireEvent(Event.New("AfterDrank")); //for glotrot. all you need is this event and glotrot seems to work intrinsically with putrefying blood
        }
    }
}