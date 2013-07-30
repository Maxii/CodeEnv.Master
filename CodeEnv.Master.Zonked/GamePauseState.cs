// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePauseState.cs
// Commands for communicating changes in the Paused state of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Commands for communicating changes in the Paused state of the game.
    /// They reflect a decision by the GameManager, unlike PauseRequest's which are requests.
    /// </summary>
    public enum GamePauseState {
        None,
        Paused,
        Resumed
    }
}

