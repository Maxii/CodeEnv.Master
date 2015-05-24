// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedItemHudContent.cs
// Content for the SelectedItemHudElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content for the SelectedItemHudElement.
    /// </summary>
    public class SelectedItemHudContent : AHudElementContent {

        public AItemReport Report { get; private set; }

        public SelectedItemHudContent(HudElementID id, AItemReport report)
            : base(id) {
            Report = report;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

