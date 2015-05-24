// --------------------------------------------------------------------------------------------------------------------
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
    using UnityEngine;

    /// <summary>
    /// Immutable report for UniverseCenterItems.
    /// </summary>
    public class UniverseCenterReport : AIntelItemReport {

        public UniverseCenterReport(UniverseCenterData data, Player player, IUniverseCenterItem item)
            : base(data, player, item) {
        }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            Name = data.Name;
            Owner = data.Owner;
            Position = data.Position;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

