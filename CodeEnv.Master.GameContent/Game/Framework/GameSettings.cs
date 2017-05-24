// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Settings.cs
// Data container holding all settings for a new or loaded game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container holding all settings for a new or loaded game.
    /// </summary>
    public class GameSettings {

        public string DebugName { get { return GetType().Name; } }

        public bool __IsStartup { get; set; }
        public bool __UseDebugCreatorsOnly { get; set; }

        /// <summary>
        /// Indicates whether to deploy additional AI creators beyond those deployed 
        /// due to the chosen player start level. These additional creators would use 
        /// randomly generated deploy dates to provide more test coverage.
        /// <remarks>Not used when __UseDebugCreatorsOnly is true.</remarks>
        /// </summary>
        public bool __DeployAdditionalAICreators { get; set; }

        public int __AdditionalFleetCreatorQty { get; set; }

        public int __AdditionalStarbaseCreatorQty { get; set; }

        public int __AdditionalSettlementCreatorQty { get; set; }


        /// <summary>
        /// Indicates whether the camera should zoom on a UserUnit
        /// when starting a game.
        /// <remarks>Not used when __UseDebugCreatorsOnly is true.</remarks>
        /// </summary>
        public bool __ZoomOnUser { get; set; }

        public bool IsSavedGame { get; set; }

        public UniverseSize UniverseSize { get; set; }

        public SystemDensity SystemDensity { get; set; }

        public int PlayerCount { get; set; }

        public Player UserPlayer { get; set; }
        public Player[] AIPlayers { get; set; }

        public EmpireStartLevel UserStartLevel { get; set; }
        public EmpireStartLevel[] AIPlayersStartLevels { get; set; }

        public SystemDesirability UserHomeSystemDesirability { get; set; }
        public SystemDesirability[] AIPlayersHomeSystemDesirability { get; set; }

        public PlayerSeparation[] AIPlayersUserSeparations { get; set; }

        public GameSettings() { }

        public override string ToString() {
            return DebugName;
        }

    }
}

