namespace Nexus.Registry
{

    /// <summary>
    /// Table of string constants for event strings to prevent typos.
    /// </summary>

    static class Events
    {

        /// <summary>
        /// Activates post-gameover behaviors in all parts associated with Vampirism.
        /// </summary>
        public const string GAMEOVER = "HumanityGameoverEventVampirism";

        /// <summary>
        /// Restores humanity and resests gameover.
        /// </summary>
        public const string WISH_HUMANITY = "WishGameOverEventVampirism";
        
        public const string UPDATE = "EventUpdateVampirism";
    }

}