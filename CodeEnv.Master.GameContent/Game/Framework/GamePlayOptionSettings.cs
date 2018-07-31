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


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data wrapper class carrying all the settings available from the GamePlayOptionsMenu.
    /// </summary>
    public class GamePlayOptionSettings {

        public string DebugName { get { return GetType().Name; } }

        public bool IsCameraRollEnabled { get; set; }
        public bool IsZoomOutOnCursorEnabled { get; set; }
        public bool IsResetOnFocusEnabled { get; set; }
        public bool IsPauseOnLoadEnabled { get; set; }
        public bool IsAiHandlesUserCmdModuleInitialDesignsEnabled { get; set; }
        public bool IsAiHandlesUserCmdModuleRefitDesignsEnabled { get; set; }
        public bool IsAiHandlesUserCentralHubInitialDesignsEnabled { get; set; }
        public bool IsAiHandlesUserElementRefitDesignsEnabled { get; set; }

        public GameSpeed GameSpeedOnLoad { get; set; }

        public GamePlayOptionSettings() { }

        public override string ToString() {
            return DebugName;
        }

    }
}

