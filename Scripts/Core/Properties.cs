using Nexus.Blood;

namespace Nexus.Properties
{
    /// <summary>
    /// Table of string constants for property strings to prevent typos.
    /// </summary>
    static class FLAGS
    {

        public static class MOD
        {
            public const string VERSION = "VampirismModVersion";
        }
        public static class EMBRACE
        {
            public const string EMBRACEABLE = "EmbraceableObject";

            public const string LEVEL_ON_DEATH = "EmbraceableObjectLevel";
        }
        /// <summary>
        /// Constants for the string literal value of BLOOD_STATUS.
        /// </summary>
        public static class BLOOD
        {
            public const string GLUT = nameof(BloodMetabolism.Glut);
            public const string QUENCHED = nameof(BloodMetabolism.Quenched);
            public const string THIRSTY = nameof(BloodMetabolism.Thirsty);
            public const string PARCHED = nameof(BloodMetabolism.Parched);
            public const string MIN = nameof(BloodMetabolism.Min);
        }
        public static string TRUE = bool.TrueString;
        public static string FALSE = bool.FalseString;
        public const string TRUE_LEGACY = "true";
        ///compatibility for when the literal was "true" instead of bool.TrueString
		//the innocent flag is immutable so anyone who played before the change
		//will have objects that have the old literal

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
        /// Immutable boolean value given to all possible feeding targets the moment they are created in the game world. Hostiles are given a value of false. True innocents can cause humanity loss.
        /// </summary>
        public const string INNOCENT = "VampirismModInnocent";

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
    }
}