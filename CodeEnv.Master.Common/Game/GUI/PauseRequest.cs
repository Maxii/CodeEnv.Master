// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PauseRequest.cs
// Commands for REQUESTING changes in the Pause state of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Commands for REQUESTING changes in the Pause state of the game.
    /// </summary>
    public enum PauseRequest {

        /// <summary>
        /// The absense of a PauseRequest.
        /// </summary>
        None,

        /// <summary>
        /// Command indicating the user or program has directly requested a GamePause. This will
        /// always be accomodated.
        /// </summary>
        PriorityPause,

        /// <summary>
        /// Command indicating a Gui element has automatically requested a GamePause.
        /// This will be accomodated if the game is running and not already paused.
        /// </summary>
        GuiAutoPause,

        /// <summary>
        /// Command indicating the user or program has directly requested an end to the current pause. This
        /// will always be accomodated.
        /// </summary>
        PriorityResume,

        /// <summary>
        /// Command indicating a Gui element has automatically requested an end to the current pause.
        /// This will only be accomodated if the game is running, it is paused, and the pause was requested by GuiAutoPause.
        /// </summary>
        GuiAutoResume

    }
}

