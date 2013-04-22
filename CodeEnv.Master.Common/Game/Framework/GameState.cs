// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameState.cs
// The primary states of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {


    /// <summary>
    /// The primary states of the game.
    /// </summary>
    public enum GameState {

        /// <summary>
        /// The absense of any GameState.
        /// </summary>
        None,

        /// <summary>
        /// Primary focus is on the user making decisions about options, new game
        /// settings or selecting a saved game to load.
        /// </summary>
        Lobby,

        /// <summary>
        /// Primary focus is on use of new game settings to do any preparation
        /// needed before initiating a Load of the game.
        /// </summary>
        Building,

        /// <summary>
        /// Primary focus is to allow Unity to load the level for either a new or previously
        /// saved game. Completion is indicated by OnLevelWasLoaded().
        /// </summary>
        Loading,

        /// <summary>
        /// Primary focus is to allow deserialization of saved games to take place so that objects in the
        /// level that has just been loaded can have their saved state restored. Completion 
        /// is indicated by OnDeserialized().
        /// </summary>
        Restoring,

        /// <summary>
        ///  The state just prior to Running, waiting for approval to Run.
        /// </summary>
        Waiting,

        /// <summary>
        /// The normal state when the user is playing a game instance. The user may initiate the save of the game, 
        /// load a saved game, launch a new game, exit the game or return to the lobby from this state.
        /// </summary>
        Running

    }
}

