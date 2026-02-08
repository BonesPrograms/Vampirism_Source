using XRL.UI;

namespace Nexus.Rules
{

    public static class MOD
    {
        public const string VERSION = "2";
    }
    static class STEALTH
    {
        public const uint AI_RADIUS = 21;
    }
    static class EMBRACE
    {
        public const string COMMAND_NAME = "CommandEmbraceSpell";
    }
    static class FLEDGLING
    {
        public const int TIMER = 500;
    }

    static class COFFIN
    {

        public const string BLUEPRINT = "Iron Maiden";
        public const string COMMAND_NAME = "invokeCoffinCMD";

        public const string ABILITY_NAME = "Invoke Coffin";

        public const int MATERIALIZE_COOLDOWN = 200;

        public const int SAVE_FROM_DEATH_MIN = 1000;

        public const int SAVE_FROM_DEATH_MAX = 5000;

        public const int SAVING_THROW_DC = 20;
    }
    static class GHOUL
    {
        public const int COOLDOWN = 500;
        public const int REGEN = 5000;
        public const string COMMAND_NAME = "CommandGhoulSpell";
        public const string ABILITY_NAME = "Ghoul";
        public const int MIN = 500;
        public const int BUFFTIME = 500;
    }

    static class OPTIONS //some of these are only referenced once but i hate string literals and i like having them all in one place so i can compare them to my XMLs to make sure there
    {                     //are no typos

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

        public static class AUTOSIP_LEVELS
        {
            public const string GLUT = "Glutted";
            public const string QUENCH = "Gorged";
            public const string THIRSTY = "Thirsty";
            public const string PARCHED = "Fiending";
            public const string MIN = "Ravenous";
        }

        public const string HUNTER = "hunterMode";
        public const string BLOOD_NERF = "vampBloodNerf";
        public const string BLEEDLIQUID = "blood-999,salt-1";
    }
    static class FEED
    {
        public const int DURATION = 5;
        public const int COOLDOWN = 50;
    }
    static class HUMANITY
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

    static class VITAE
    {

        public const int BLOOD_MIN = 0;
        public const int BLOOD_PARCHED = 10000;
        public const int BLOOD_THIRSTY = 20000;
        public const int BLOOD_QUENCHED = 30000;
        public const int BLOOD_GLUTTONOUS = 40000;
        public const int BLOOD_PUKE = 50000; //equivelant storage values to a stomach

        public static int BLOOD_METAB => Options.GetOptionBool(OPTIONS.HUNTER) ? METAB_TYPES.HUNTER : METAB_TYPES.DEFAULT;
        static class METAB_TYPES
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

        public const int BLOOD_PER_BLOODLOSS = 500;
        public const int BLOOD_PER_BLOODLOSS_FEED = 500; //used to be a different number but im experimenting with values rn, may become a diff number again one day

    }
}