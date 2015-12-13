// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PauseCommand.cs
// Commands signaling a desired change in the PauseState of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Commands signaling a desired change in the PauseState of the game.
    /// </summary>
    public enum PauseCommand {

        /// <summary>
        /// For error checking.
        /// </summary>
        None,

        Pause,

        Resume

        /// <summary>
        /// The player has manually requested a pause in game play.
        /// This command will always be accommodated.
        /// </summary>
        //ManualPause,

        /// <summary>
        /// The game has automatically requested a pause in game play.
        /// Typically this is caused by a Gui Menu, Screen or Popup opening.
        /// This command will be accommodated if not already paused.
        /// </summary>
        //AutoPause,

        /// <summary>
        /// The player has manually requested a resumption of game play. 
        /// This command will always be accommodated.
        /// </summary>
        //ManualResume,

        /// <summary>
        /// The game has automatically requested a resumption of game play.
        /// Typically this is caused by the last remaining Gui Menu, Screen or Popup closing.
        /// This command will only be accommodated if the game was previously paused by the AutoPause command.
        /// </summary>
        //AutoResume

    }
}

