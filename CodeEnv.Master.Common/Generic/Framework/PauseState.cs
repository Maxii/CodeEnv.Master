// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PauseState.cs
// The paused state of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// The Paused state of the game.
    /// </summary>
    public enum PauseState {

        /// <summary>
        /// For error checking.
        /// </summary>
        None,

        /// <summary>
        /// The game is not paused.
        /// </summary>
        NotPaused,

        /// <summary>
        /// The game was automatically paused.
        /// Typically this is caused by a Gui Menu, Screen or Popup opening.
        /// </summary>
        AutoPaused,

        /// <summary>
        /// The game was paused by the player.
        /// Typically, this is caused by the player using the Pause button or key.
        /// </summary>
        Paused

    }
}

