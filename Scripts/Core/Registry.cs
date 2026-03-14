namespace VampirismSys.Registry
{

    /// <summary>
    /// Table of string constants for event strings to prevent typos.
    /// </summary>

    internal static class Events
    {

        /// <summary>
        /// Activates post-gameover behaviors in all parts associated with Vampirism.
        /// </summary>
        internal const string GAMEOVER = "HumanityGameoverEventVampirism";

        /// <summary>
        /// Restores humanity and resests gameover.
        /// </summary>
        internal const string WISH_HUMANITY = "WishGameOverEventVampirism";
        
        internal const string UPDATE = "EventUpdateVampirism";
    }

}