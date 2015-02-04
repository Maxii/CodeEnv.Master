// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorReport.cs
//  Immutable report on a sector reflecting a specific player's IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Immutable report on a sector reflecting a specific player's IntelCoverage.
    /// </summary>
    public class SectorReport : AItemReport {

        public Index3D? SectorIndex { get; private set; }

        public float? Density { get; private set; }

        public SectorReport(SectorData data, Player player)
            : base(data, player) { }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageMinimal(data);
            var sData = data as SectorData;
            Density = sData.Density;
        }

        protected override void AssignIncrementalValues_IntelCoverageNone(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageNone(data);
            var sData = data as SectorData;
            Name = sData.Name;
            SectorIndex = sData.SectorIndex;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

