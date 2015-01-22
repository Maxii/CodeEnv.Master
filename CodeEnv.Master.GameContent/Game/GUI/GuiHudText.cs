// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
    /// </summary>
    public class GuiHudText : GuiLabelText {

        public IntelCoverage IntelCoverage { get; private set; }

        public GuiHudText(IntelCoverage intelCoverage)
            : base(isDedicatedLinePerKey: true) {
            IntelCoverage = intelCoverage;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

