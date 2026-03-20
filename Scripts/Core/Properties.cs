using System;
using VampirismSys.Blood;
using XRL.World.Parts;

namespace VampirismSys.Properties
{
    /// <summary>
    /// Table of string constants for property strings to prevent typos.
    /// </summary>
    public static class Flags
    {

        public static class Mod
        {
            public const string ZONE_VERSION_TAG = "VampirismModVersion";
            public const string GAMEOBJECT_VERSION_TAG = "GameObjVersionVampirsmMod";

            public const string OLD_SAVE = "ActualOldVampirismSave";
        }
        public static class Embrace
        {
            public const string EMBRACEABLE = "EmbraceableObject";

            public const string LEVEL_ON_DEATH = "EmbraceableObjectLevel";
        }
        /// <summary>
        /// Constants for the string literal value of BLOOD_STATUS.
        /// </summary>
        public static class Blood
        {
            public const string GLUT = nameof(BaseBloodMetabolism.Glut);

            public const string QUENCHED = nameof(BaseBloodMetabolism.Quenched);

            public const string THIRSTY = nameof(BaseBloodMetabolism.Thirsty);

            public const string PARCHED = nameof(BaseBloodMetabolism.Parched);

            public const string MIN = nameof(BaseBloodMetabolism.Min);
        }
        public static string TRUE = bool.TrueString;

        public static string FALSE = bool.FalseString;

        public const string SPELLS = "HasVampirismSpells";

        public const string COFFIN = "VampireCoffinSourceID";

        /// <summary>
        /// Boolean.
        /// </summary>
        public const string FRENZY = "VampirismModFrenzying";

        /// <summary>
        /// Boolean.
        /// </summary>
        public const string FEED = "VampirismModFeeding";

        /// <summary>
        /// Boolean.
        /// </summary>
        public const string STEALTH = "VampirismModStealthy";
        /// <summary>
        /// Boolean.
        /// </summary>
        public const string GO = "VampirismModHumanityGameover";


        /// <summary>
        /// The simplified string value of blood for UI display and Frenzy chances.
        /// </summary>
        public const string BLOOD_STATUS = "VampirismModBlooddrinker";
        /// <summary>
        /// The integer value for blood.
        /// </summary>
        public const string BLOOD_VALUE = "VampirismModVitae";
        /// <summary>
        /// The integer value for humanity score.
        /// </summary>
        public const string HUMANITY = "VampirismModHumanity";
        /// <summary>
        /// The integer value for humanity's regeneration.
        /// </summary>
        public const string REGEN = "VampirismModHumanityRegen";


        /// <summary>
        /// Immutable boolean value given to all possible feeding targets the moment they are created in the game world. Hostiles are given a value of false. True innocents can cause humanity loss in various ways as a result of being fed on (only feeding related).
        /// </summary>
        public const string INNOCENT = "VampirismModInnocent"; //yes huge note - only feeding related - humanity's death consequences do not actually check for innocence, only IFeeding does
                                                                 //which it uses to assign VICTIM, which is further checked in deaths
                                                                 // LONG VALUES
        /// <summary>
        /// Flag given to true innocents after feeding has ended, with a long value of marking the moment feed ended in game turn time. for DeathHandler.
        /// </summary>
        public const string VICTIM = "VampirismModVictim";
        /// <summary>
        /// Flag given to false innocents, who are currently companions, after feeding has ended. Allows humanity loss until they are no longer companions. For DeathHandler.
        /// </summary>
        public const string VICTIM_HOSTILE = "VampirismModHostileVictim";
        /// <summary>
        /// Special flag used to prevent DeathEventHandler from duplicating humanity losses if a true innocent or companion dies during feeding, due to the automatic application of VICTIM on feed removal.
        /// </summary>
        public const string DEAD = "VampirismModKilledDuringFeed";

        public const string FLEDGLING = "VampireFledgingMod";




    }
}