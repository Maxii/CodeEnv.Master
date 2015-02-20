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

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SystemItems.
    /// </summary>
    public class SystemReport : AItemReport {

        public Index3D SectorIndex { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }

        public StarReport StarReport { get; private set; }
        public SettlementReport SettlementReport { get; private set; }
        public PlanetoidReport[] PlanetoidReports { get; private set; }

        public SystemReport(SystemData data, Player player, StarReport starReport, SettlementReport settlementReport, PlanetoidReport[] planetoidReports)
            : base(player) {
            StarReport = starReport;
            SettlementReport = settlementReport;
            PlanetoidReports = planetoidReports;
            AssignValues(data);
        }

        protected override void AssignValues(AItemData data) {
            var sysData = data as SystemData;
            Name = sysData.Name;
            SectorIndex = sysData.SectorIndex;
            Owner = SettlementReport != null ? SettlementReport.Owner : null;        // IMPROVE NoPlayer?, other Settlement info?
            Capacity = StarReport.Capacity + PlanetoidReports.Sum(pr => pr.Capacity);
            Resources = StarReport.Resources.Sum(PlanetoidReports.Select(pr => pr.Resources).ToArray());
            SpecialResources = StarReport.SpecialResources.Sum(PlanetoidReports.Select(pr => pr.SpecialResources).ToArray());
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

