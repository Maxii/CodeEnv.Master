// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OptionSettings.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class OptionSettings {

        public bool IsCameraRollEnabled { get; set; }
        public bool IsZoomOutOnCursorEnabled { get; set; }
        public bool IsResetOnFocusEnabled { get; set; }
        public bool IsPauseOnLoadEnabled { get; set; }

        public GameClockSpeed GameSpeedOnLoad { get; set; }

        public OptionSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

