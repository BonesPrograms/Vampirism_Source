using XRL.UI;

namespace VampirismSys.Rules
{


    public static class Batform
    {
        public const string COMMAND_NAME = "invokeBatform";

        public const string ABILITY_NAME = "Batform";

        public const int COOLDOWN = 50;

        public const string FACTION = "Winged Mammals";
    }
    public static class Mod
    {
        public const string VERSION = "4";
    }
    public static class Stealth
    {
        public const uint AI_RADIUS = 21;
    }
    public static class Embrace
    {
        public const string COMMAND_NAME = "CommandEmbraceSpell";

        public const string ABILITY_NAME = "Embrace";

        public const int COOLDOWN = 1000;
    }
    public static class Coffin
    {

        public const string BLUEPRINT = "VampireCoffin";
        public const string COMMAND_NAME = "invokeCoffinCMD";

        public const string ABILITY_NAME = "Invoke Coffin";

        public const int MATERIALIZE_COOLDOWN = 500;

        public const int SAVE_FROM_DEATH_MIN = 3000;

        public const int SAVE_FROM_DEATH_MAX = 5000;

        public const int SAVING_THROW_DC = 20;
    }
    public static class Ghoul
    {
        public const int COOLDOWN = 500;
        public const int REGEN = 5000;
        public const string COMMAND_NAME = "CommandGhoulSpell";
        public const string ABILITY_NAME = "Ghoul";
        public const int MIN = 500;
        public const int BUFFTIME = 500;
    }

    public static class ModOptions //some of these are only referenced once but i hate string literals and i like having them all in one place so i can compare them to my XMLs to make sure there
    {                     //are no typos

        public const string TRUE_UNDEAD = "VampirismTrueUndead";
        public const string BLEED_THIRST = "VampireBleedThirst";
        public const string SILVER = "SilverAilment";
        public const string COFFIN = "CoffinAutoWin";
        public const string TORCH = "FearOfTorches";
        public const string FRACTUS_NERF = "FractusNerf";
        public const string HUMANITY = "humanity";
        public const string FRENZY = "frenzy";
        public const string AUTOGET = "BloodAutoget";
        public const string BLOODPACK = "StartingBloodPack";
        public const string REDTEXT = "RedBloodTextVampirism";
        public const string SPELLS = "enablevampiricpowers";
        public const string AUTOSIP = "BloodAutosip";

        public const string FIRE = "FearOfFire";

        public const string DOUG = "UnderDougsHum";

        public const string NIGHTBEAST = "NightbeastMode";

        public const string AUTOSIP_LEVEL = "BloodAutosipLevel";

        public static class Autosip_Settings
        {
            public const string QUENCH = "Gorged";
            public const string THIRSTY = "Thirsty";
            public const string PARCHED = "Fiending";
            public const string MIN = "Ravenous";
        }

        public const string HUNTER = "hunterMode";
        public const string BLOOD_NERF = "vampBloodNerf";
        public const string BLEEDLIQUID = "blood-999,salt-1";
    }
    public static class Feed
    {
        public const int DURATION = 5;
        public const int COOLDOWN = 50;
    }
    public static class Hum
    {
        public const int REGEN_TIME = 5000;
        public const int REGEN = 1;
        public const int LOSS_PER_KILL = 1;
        public const int GAMEOVER = 0;
        public const int CRIT = 1;
        public const int LOW = 2;
        public const int MID = 3;
        public const int HIGH = 4;
        public const int MAX = 5;

    }

    public static class Metab
    {

        public const int BLOOD_MIN = 0;
        public const int BLOOD_PARCHED = 10000;
        public const int BLOOD_THIRSTY = 20000;
        public const int BLOOD_QUENCHED = 30000;
        public const int BLOOD_GLUTTONOUS = 40000;
        public const int BLOOD_PUKE = 50000; //equivelant storage values to a stomach

        public static int BLOOD_METAB => Options.GetOptionBool(ModOptions.HUNTER) ? Metab_Settings.HUNTER : Metab_Settings.DEFAULT;
        public static class Metab_Settings
        {
            public const int HUNTER = 5;
            public const int DEFAULT = 20; //same value for stomach water metab. funny cause in decompiled code, appears to just be a magic number!!!!
        }
        public const int BLOOD_PER_SIP = 10000; //this is the amount of water you get when drinking a sip of water as a non vampire
        public const int BLOOD_PER_FEED = 2000; //balanced for the 5 duration turn thing: 5 x 2000 == 10,000
        public const int BLOOD_PER_GHOUL = BLOOD_PER_FEED * 2;
        public const int FEED_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_FEED;
        public const int SIP_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_SIP;

        public const int GHOUL_PUKE_WARN = BLOOD_PUKE - BLOOD_PER_GHOUL;

        public const int BLOOD_PERBloodLOSS = 100;
        public const int BLOOD_PERBloodLOSS_FEED = 100; //used to be a different number but im experimenting with values rn, may become a diff number again one day
                                                        //used to be 500 but i got complaints so now its 100
    }
}