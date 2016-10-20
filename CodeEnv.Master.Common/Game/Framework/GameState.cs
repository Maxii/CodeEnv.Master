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
        /// The absence of any GameState.
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

        ////Waiting,

        /// <summary>
        /// Deploys all SystemCreators that need to be deployed programmatically. 
        /// It is OK if some system creators have already been deployed manually.
        /// </summary>
        DeployingSystemCreators,

        /// <summary>
        /// Build out all Systems from the deployed SystemCreators. The systems need to be built at their
        /// deployed position before the path graph can be generated.
        /// </summary>
        BuildingSystems,

        /// <summary>
        /// Primary focus is to allow the AStar Pathfinding system time to generate the overall graph
        /// from points acquired from SectorGrid's GridFramework.
        /// </summary>
        GeneratingPathGraphs,

        /// <summary>
        /// Design and record all initial unit designs in preparation for building and deploying the units into the universe during
        /// BuildingAndDeployingInitialUnits. Unit operations do not commence until on or after Running.
        /// </summary>
        DesigningInitialUnits,

        /// <summary>
        /// Build and deploy all starting units to their starting location in the universe. 
        /// </summary>
        BuildingAndDeployingInitialUnits,

        /// <summary>
        /// The final countdown step in progression prior to Running.
        /// </summary>
        PreparingToRun,

        /// <summary>
        /// The normal state when the user is playing a game instance. The user may initiate the save of the game, 
        /// load a saved game, launch a new game, exit the game or return to the lobby from this state.
        /// </summary>
        Running

    }
}

