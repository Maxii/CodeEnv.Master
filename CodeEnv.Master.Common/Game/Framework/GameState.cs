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
        /// Primary focus is to allow Unity to load the level for either a new or previously
        /// saved game. If the level is for a new game, the new game scene is loaded which 
        /// will be populated during Building. If the level is from a previously saved game, 
        /// the level loaded is the level that was saved which will be populated during Restoring.
        /// OnLevelWasLoaded() occurs during this state which finishes the state and allows
        /// the game state to progress.
        /// </summary>
        Loading,

        /// <summary>
        /// Primary focus is on use of new game settings to add the elements of the
        /// new game to the newly loaded new game scene.
        /// </summary>
        Building,

        /// <summary>
        /// Primary focus is to allow deserialization of saved games so that objects in the
        /// level that has just been loaded can have their saved state restored. OnDeserialized()
        /// occurs during this state which finishes the state and allows the game state to 
        /// progress.
        /// </summary>
        Restoring,

        /// <summary>
        ///  A waiting state to allow other players to signal their readiness to progress the state.
        /// </summary>
        Waiting,

        /// <summary>
        /// Build, initialize and deploy all Systems and other potential pathfinding obstacles prior to generating the network
        /// of waypoints known as a PathGraph. Planetoid operations donot commence until Running.
        /// </summary>
        BuildAndDeploySystems,

        /// <summary>
        /// Primary focus is to allow the AStar Pathfinding system time to generate the overall graph
        /// from points acquired from SectorGrid's GridFramework.
        /// </summary>
        GeneratingPathGraphs,

        /// <summary>
        /// Build and initialize all starting units in preparation for deployment into the universe during
        /// DeployingUnits. Unit operations donot commence until on or after Running.
        /// </summary>
        PrepareUnitsForDeployment,

        /// <summary>
        /// Deploy all initialized units to their starting location in the universe. Currently, a physical
        /// change in location occurs only for Settlements as they are assigned to Systems.
        /// </summary>
        DeployingUnits,

        /// <summary>
        /// The final countdown step in progression prior to Running.
        /// </summary>
        RunningCountdown_1,

        /// <summary>
        /// The normal state when the user is playing a game instance. The user may initiate the save of the game, 
        /// load a saved game, launch a new game, exit the game or return to the lobby from this state.
        /// </summary>
        Running

    }
}

