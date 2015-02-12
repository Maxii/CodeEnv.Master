// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorReport.cs
// Immutable report on a sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Immutable report on a sector.
    /// </summary>
    public class SectorReport : AItemReport {

        public Index3D? SectorIndex { get; private set; }

        public float? Density { get; private set; }

        public SectorReport(SectorData data, Player player)
            : base(player) {
            AssignValues(data);
        }

        protected override void AssignValues(AItemData data) {
            var sData = data as SectorData;
            Name = sData.Name;
            Owner = sData.Owner;
            SectorIndex = sData.SectorIndex;
            Density = sData.Density;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

