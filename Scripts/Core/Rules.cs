using XRL.UI;

namespace VampirismSys.Rules
{


    internal static class Batform
    {
        internal const string COMMAND_NAME = "invokeBatform";

        internal const string ABILITY_NAME = "Batform";

        internal const int COOLDOWN = 50;

        internal const string FACTION = "Winged Mammals";
    }
    internal static class Mod
    {
        internal const string VERSION = "4";
    }
    internal static class Stealth
    {
        internal const uint AI_RADIUS = 21;
    }
    internal static class Embrace
    {
        internal const string COMMAND_NAME = "CommandEmbraceSpell";

        internal const string ABILITY_NAME = "Embrace";

        internal const int COOLDOWN = 1000;
    }
    internal static class Coffin
    {

        internal const string BLUEPRINT = "VampireCoffin";
        internal const string COMMAND_NAME = "invokeCoffinCMD";

        internal const string ABILITY_NAME = "Invoke Coffin";

        internal const int MATERIALIZE_COOLDOWN = 500;

        internal const int SAVE_FROM_DEATH_MIN = 3000;

        internal const int SAVE_FROM_DEATH_MAX = 5000;

        internal const int SAVING_THROW_DC = 20;
    }
    internal static class Ghoul
    {
        internal const int COOLDOWN = 500;
        internal const int REGEN = 5000;
        internal const string COMMAND_NAME = "CommandGhoulSpell";
        internal const string ABILITY_NAME = "Ghoul";
        internal const int MIN = 500;
        internal const int BUFFTIME = 500;
    }

    internal static class ModOptions //some of these are only referenced once but i hate string literals and i like having them all in one place so i can compare them to my XMLs to make sure there
    {                     //are no typos

        internal const string TRUE_UNDEAD = "VampirismTrueUndead";
        internal const string BLEED_THIRST = "VampireBleedThirst";
        internal const string SILVER = "SilverAilment";
        internal const string COFFIN = "CoffinAutoWin";
        internal const string TORCH = "FearOfTorches";
        internal const string FRACTUS_NERF = "FractusNerf";
        internal const string HUMANITY = "humanity";
        internal const string FRENZY = "frenzy";
        internal const string AUTOGET = "BloodAutoget";
        internal const string BLOODPACK = "StartingBloodPack";
        internal const string REDTEXT = "RedBloodTextVampirism";
        internal const string SPELLS = "enablevampiricpowers";
        internal const string AUTOSIP = "BloodAutosip";

        internal const string FIRE = "FearOfFire";

        internal const string DOUG = "UnderDougsHum";

        internal const string NIGHTBEAST = "NightbeastMode";

        internal const string AUTOSIP_LEVEL = "BloodAutosipLevel";

        internal static class Autosip_Settings
        {
            internal const string QUENCH = "Gorged";
            internal const string THIRSTY = "Thirsty";
            internal const string PARCHED = "Fiending";
            internal const string MIN = "Ravenous";
        }

        internal const string HUNTER = "hunterMode";
        internal const string BLOOD_NERF = "vampBloodNerf";
        internal const string BLEEDLIQUID = "blood-999,salt-1";
    }
    internal static class Feed
    {
        internal const int DURATION = 5;
        internal const int COOLDOWN = 50;
    }
    internal static class Hum
    {
        internal const int REGEN_TIME = 5000;
        internal const int REGEN = 1;
        internal const int LOSS_PER_KILL = 1;
        internal const int GAMEOVER = 0;
        internal const int CRIT = 1;
        internal const int LOW = 2;
        internal const int MID = 3;
        internal const int HIGH = 4;
        internal const int MAX = 5;

    }

    internal static class Metab
    {

        internal const int BLOOD_MIN = 0;
        internal const int BLOOD_PARCHED = 10000;
        internal const int BLOOD_THIRSTY = 20000;
        internal const int BLOOD_QUENCHED = 30000;
        internal const int BLOOD_GLUTTONOUS = 40000;
        internal const int BLOOD_PUKE = 50000; //equivelant storage values to a stomach

        internal static int BLOOD_METAB => Options.GetOptionBool(ModOptions.HUNTER) ? Metab_Settings.HUNTER : Metab_Settings.DEFAULT;
        internal static class Metab_Settings
        {
            internal const int HUNTER = 5;
            internal const int DEFAULT = 20; //same value for stomach water metab. funny cause in decompiled code, appears to just be a magic number!!!!
        }
        internal const int BLOOD_PER_SIP = 10000; //this is the amount of water you get when drinking a sip of water as a non vampire
        internal const int BLOOD_PER_FEED = 2000; //balanced for the 5 duration turn thing: 5 x 2000 == 10,000
        internal const int BLOOD_PER_GHOUL = BLOOD_PER_FEED * 2;
        internal const int FEED_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_FEED;
        internal const int SIP_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_SIP;

        internal const int GHOUL_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_GHOUL;

        internal const int BLOOD_PERBloodLOSS = 100;
        internal const int BLOOD_PERBloodLOSS_FEED = 100; //used to be a different number but im experimenting with values rn, may become a diff number again one day
                                                        //used to be 500 but i got complaints so now its 100
    }
}