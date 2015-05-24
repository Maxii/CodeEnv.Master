// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemReport.cs
// Immutable report for SystemItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SystemItems.
    /// </summary>
    public class SystemReport : AItemReport {

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public Index3D SectorIndex { get; private set; }

        public StarReport StarReport { get; private set; }
        public SettlementReport SettlementReport { get; private set; }
        public PlanetoidReport[] PlanetoidReports { get; private set; }

        public SystemReport(SystemData data, Player player, ISystemItem item)
            : base(player, item) {
            StarReport = item.GetStarReport(player);
            SettlementReport = item.GetSettlementReport(player);
            PlanetoidReports = item.GetPlanetoidReports(player);
            AssignValues(data);
            AssignValuesFromMemberReports();
        }

        protected override void AssignValues(AItemData data) {
            var sysData = data as SystemData;
            Name = sysData.Name;
            SectorIndex = sysData.SectorIndex;
            Position = sysData.Position;
        }

        private void AssignValuesFromMemberReports() {
            Owner = SettlementReport != null ? SettlementReport.Owner : null;        // IMPROVE NoPlayer?, other Settlement info?
            Capacity = StarReport.Capacity.NullableSum(PlanetoidReports.Select(pr => pr.Capacity).ToArray());
            Resources = StarReport.Resources.NullableSum(PlanetoidReports.Select(pr => pr.Resources).ToArray());
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

