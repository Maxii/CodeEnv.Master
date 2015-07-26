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

        public bool __IsStartupSimulation { get; set; }
        public bool IsSavedGame { get; set; }

        public UniverseSize UniverseSize { get; set; }

        public int PlayerCount { get; set; }

        public Player UserPlayer { get; set; }
        public Player[] AIPlayers { get; set; }

        public GameSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

