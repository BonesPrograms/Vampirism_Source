using System;
using VampirismSys.Blood;
using XRL.World.Parts;

namespace VampirismSys.Properties
{
    /// <summary>
    /// Table of string constants for property strings to prevent typos.
    /// </summary>
    internal static class Flags
    {

        internal static class Mod
        {
            internal const string ZONE_VERSION_TAG = "VampirismModVersion";
            internal const string GAMEOBJECT_VERSION_TAG = "GameObjVersionVampirsmMod";

            internal const string OLD_SAVE = "ActualOldVampirismSave";
        }
        internal static class Embrace
        {
            internal const string EMBRACEABLE = "EmbraceableObject";

            internal const string LEVEL_ON_DEATH = "EmbraceableObjectLevel";
        }
        /// <summary>
        /// Constants for the string literal value of BLOOD_STATUS.
        /// </summary>
        internal static class Blood
        {
            internal const string GLUT = nameof(BaseBloodMetabolism.Glut);

            internal const string QUENCHED = nameof(BaseBloodMetabolism.Quenched);

            internal const string THIRSTY = nameof(BaseBloodMetabolism.Thirsty);

            internal const string PARCHED = nameof(BaseBloodMetabolism.Parched);

            internal const string MIN = nameof(BaseBloodMetabolism.Min);
        }
        internal static string TRUE = bool.TrueString;

        internal static string FALSE = bool.FalseString;

        internal const string SPELLS = "HasVampirismSpells";

        internal const string COFFIN = "VampireCoffinSourceID";

        /// <summary>
        /// Boolean.
        /// </summary>
        internal const string FRENZY = "VampirismModFrenzying";

        /// <summary>
        /// Boolean.
        /// </summary>
        internal const string FEED = "VampirismModFeeding";

        /// <summary>
        /// Boolean.
        /// </summary>
        internal const string STEALTH = "VampirismModStealthy";
        /// <summary>
        /// Boolean.
        /// </summary>
        internal const string GO = "VampirismModHumanityGameover";


        /// <summary>
        /// The simplified string value of blood for UI display and Frenzy chances.
        /// </summary>
        internal const string BLOOD_STATUS = "VampirismModBlooddrinker";
        /// <summary>
        /// The integer value for blood.
        /// </summary>
        internal const string BLOOD_VALUE = "VampirismModVitae";
        /// <summary>
        /// The integer value for humanity score.
        /// </summary>
        internal const string HUMANITY = "VampirismModHumanity";
        /// <summary>
        /// The integer value for humanity's regeneration.
        /// </summary>
        internal const string REGEN = "VampirismModHumanityRegen";


        /// <summary>
        /// Immutable boolean value given to all possible feeding targets the moment they are created in the game world. Hostiles are given a value of false. True innocents can cause humanity loss in various ways as a result of being fed on (only feeding related).
        /// </summary>
        internal const string INNOCENT = "VampirismModInnocent"; //yes huge note - only feeding related - humanity's death consequences do not actually check for innocence, only IFeeding does
                                                                 //which it uses to assign VICTIM, which is further checked in deaths
                                                                 // LONG VALUES
        /// <summary>
        /// Flag given to true innocents after feeding has ended, with a long value of marking the moment feed ended in game turn time. for DeathHandler.
        /// </summary>
        internal const string VICTIM = "VampirismModVictim";
        /// <summary>
        /// Flag given to false innocents, who are currently companions, after feeding has ended. Allows humanity loss until they are no longer companions. For DeathHandler.
        /// </summary>
        internal const string VICTIM_HOSTILE = "VampirismModHostileVictim";
        /// <summary>
        /// Special flag used to prevent DeathEventHandler from duplicating humanity losses if a true innocent or companion dies during feeding, due to the automatic application of VICTIM on feed removal.
        /// </summary>
        internal const string DEAD = "VampirismModKilledDuringFeed";

        internal const string FLEDGLING = "VampireFledgingMod";




    }
}