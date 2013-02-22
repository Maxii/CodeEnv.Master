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
        /// Reached either at the launch of the Unity Application or from Running,
        /// this state represents the absense of any game instance. New or saved game
        /// data can exist in this state as one or the other is required to move to Building.
        /// </summary>
        PreGame,

        /// <summary>
        /// Reached only from PreGame, new or saved game data is used to build the 
        /// world. Completion of the act of building the world advances the state to WaitingForPlayers.
        /// </summary>
        Building,

        /// <summary>
        /// Reached only from Building, this state continues until all the players have indicated
        /// their readiness, each via a PlayerReady event (TODO). When all players have signalled
        /// their readiness, the state moves to Launching.
        /// </summary>
        WaitingForPlayers,

        /// <summary>
        /// Reached only from WaitingForPlayers, .... When all parts of the game engine indicate
        /// their readiness, the state advances to Running.
        /// </summary>
        Launching,

        /// <summary>
        /// Reached only from Launching, this is the normal state when the user is playing
        /// a game instance. Receipt of a AppTermination, CreateNewGameBegun or LoadGameBegun
        /// event (all TODO), cause the termination of this game instance, resulting in a return to
        /// the PreGame state.
        /// </summary>
        Running

    }
}

