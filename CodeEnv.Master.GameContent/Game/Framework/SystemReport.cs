// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemReport.cs
// Immutable report on a system reflecting what a specific player's knows
// about the planetoids, star and settlement of the system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report on a system reflecting what a specific player's knows
    ///  about the planetoids, star and settlement of the system.
    /// </summary>
    public class SystemReport : AItemReport {

        // TODO Include Settlement in aggregation

        public Index3D SectorIndex { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }

        public StarReport StarReport { get; private set; }
        public PlanetoidReport[] PlanetoidReports { get; private set; }

        public SystemReport(SystemItemData data, Player player, StarReport starReport, PlanetoidReport[] planetoidReports)
            : base(player) {
            StarReport = starReport;
            PlanetoidReports = planetoidReports;
            AssignValues(data, starReport, planetoidReports);
        }

        private void AssignValues(SystemItemData data, StarReport starReport, PlanetoidReport[] planetoidReports) {
            Name = data.Name;
            SectorIndex = data.SectorIndex;
            Owner = data.Owner;
            Capacity = starReport.Capacity + planetoidReports.Sum(pr => pr.Capacity);
            Resources = starReport.Resources.Sum(planetoidReports.Select(pr => pr.Resources).ToArray());
            SpecialResources = starReport.SpecialResources.Sum(planetoidReports.Select(pr => pr.SpecialResources).ToArray());
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

