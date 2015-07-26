// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedItemHudFormContent.cs
// Content for the SelectedItemHudForm.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content for the SelectedItemHudForm.
    /// </summary>
    [System.Obsolete]
    public class SelectedItemHudFormContent : AHudFormContent {

        public AItemReport Report { get; private set; }

        public SelectedItemHudFormContent(HudFormID id, AItemReport report)
            : base(id) {
            Report = report;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

