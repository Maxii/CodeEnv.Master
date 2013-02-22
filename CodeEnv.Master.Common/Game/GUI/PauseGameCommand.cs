// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PauseGameCommand.cs
// Commands for communicating changes in the Pause state of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Commands for communicating changes in the Pause state of the game.
    /// They reflect a decision by the GameManager, unlike GuiPauseCommand's which are requests.
    /// </summary>
    public enum PauseGameCommand {
        None,
        Pause,
        Resume
    }
}

