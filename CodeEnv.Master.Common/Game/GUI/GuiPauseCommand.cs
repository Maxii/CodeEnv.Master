// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseCommand.cs
// Gui Commands for REQUESTING pause-related events from the GameManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Gui Commands for REQUESTING pause-related events from the GameManager.
    /// </summary>
    public enum GuiPauseCommand {

        /// <summary>
        /// The absense of a GuiPauseCommand.
        /// </summary>
        None,

        /// <summary>
        /// Command indicating a user has directly requested a GamePause. This will
        /// always be accomodated.
        /// </summary>
        UserPause,

        /// <summary>
        /// Command indicating a Gui element has automatically requested a GamePause.
        /// This will be accomodated if the game is not already paused.
        /// </summary>
        GuiAutoPause,

        /// <summary>
        /// Command indicating a user has directly requested an end to the current pause. This
        /// will always be accomodated.
        /// </summary>
        UserResume,

        /// <summary>
        /// Command indicating a Gui element has automatically requested an end to the current pause.
        /// This will be accomodated if the game is not currently in a UserPause.
        /// </summary>
        GuiAutoResume

    }
}

