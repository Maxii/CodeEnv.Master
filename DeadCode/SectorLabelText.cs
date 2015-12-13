// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorLabelText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a Label for a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a Label for a Sector.
    /// </summary>
    [Obsolete]
    public class SectorLabelText : ALabelText {

        public SectorLabelText(DisplayTargetID displayTgtID, SectorReport report, bool isDedicatedLinePerContentID) : base(displayTgtID, report, isDedicatedLinePerContentID) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

