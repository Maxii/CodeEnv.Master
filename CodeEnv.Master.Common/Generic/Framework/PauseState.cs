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

        None,

        /// <summary>
        /// The Game is not paused.
        /// </summary>
        NotPaused,

        /// <summary>
        /// The game is temporarily paused due to a Gui Element being displayed.
        /// </summary>
        GuiAutoPaused,

        /// <summary>
        /// The game is paused either by the user or by the game.
        /// </summary>
        Paused

    }
}

