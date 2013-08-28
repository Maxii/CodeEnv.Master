// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePlayOptionSettings.cs
// Data wrapper class carrying all the settings available from the GamePlayOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Data wrapper class carrying all the settings available from the GamePlayOptionsMenu.
    /// </summary>
    public class GamePlayOptionSettings {

        public bool IsCameraRollEnabled { get; set; }
        public bool IsZoomOutOnCursorEnabled { get; set; }
        public bool IsResetOnFocusEnabled { get; set; }
        public bool IsPauseOnLoadEnabled { get; set; }

        public GameClockSpeed GameSpeedOnLoad { get; set; }

        public GamePlayOptionSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

