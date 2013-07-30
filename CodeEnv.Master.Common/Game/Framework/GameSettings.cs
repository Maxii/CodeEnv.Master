// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Settings.cs
// Data container class holding all settings for a new or loaded game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Data container class holding all settings for a new or loaded game.
    /// </summary>
    public class GameSettings {

        public bool IsNewGame { get; set; }
        public UniverseSize UniverseSize { get; set; }
        public Race PlayerRace { get; set; }

        public GameSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

