﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LabelText.cs
// Wrapper class for a StringBuilder that holds text for display in Labels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds text for display in Labels.
    /// This derived class supports AItems, aka Items that have no PlayerIntel.
    /// </summary>
    public class LabelText : ALabelText {

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelLabelText" /> class.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <param name="report">The report.</param>
        /// <param name="isDedicatedLinePerContentID">if set to <c>true</c> the text associated with each key will be displayed on a separate line.</param>
        public LabelText(LabelID labelID, AReport report, bool isDedicatedLinePerContentID)
            : base(labelID, report, isDedicatedLinePerContentID) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

