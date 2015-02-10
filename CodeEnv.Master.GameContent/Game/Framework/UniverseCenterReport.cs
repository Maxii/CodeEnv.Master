﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterReport.cs
// Immutable report for UniverseCenterItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for UniverseCenterItems.
    /// </summary>
    public class UniverseCenterReport : AIntelItemReport {

        public UniverseCenterReport(UniverseCenterItemData data, Player player)
            : base(data, player) {
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageAware(data);
            Name = data.Name;
            Owner = data.Owner;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

