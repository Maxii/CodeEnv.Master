// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetReport.cs
// Immutable report on a fleet reflecting a specific player's IntelCoverage of
// the fleet's command and its elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report on a fleet reflecting a specific player's IntelCoverage of
    /// the fleet's command and its elements.
    /// </summary>
    public class FleetReport : ACmdReport {

        public FleetCategory Category { get; private set; }

        public FleetUnitComposition? UnitComposition { get; private set; }

        public INavigableTarget Target { get; private set; }

        public float? CurrentSpeed { get; private set; }

        //public float RequestedSpeed { get; private set; }
        //public Vector3 RequestedHeading { get; private set; }
        //public Vector3 CurrentHeading { get; private set; }

        public float? UnitFullSpeed { get; private set; }

        //public float UnitFullStlSpeed { get; private set; }
        //public float UnitFullFtlSpeed { get; private set; }

        public float? UnitMaxTurnRate { get; private set; }


        public FleetReport(FleetCmdData cmdData, Player player, ShipReport[] shipReports)
            : base(cmdData, player, shipReports) { }

        protected override void AssignValuesFrom(AElementItemReport[] elementReports, ACommandData cmdData) {
            base.AssignValuesFrom(elementReports, cmdData);
            var knownElementCategories = elementReports.Cast<ShipReport>().Select(r => r.Category).Where(cat => cat != ShipCategory.None);
            UnitComposition = new FleetUnitComposition(knownElementCategories);
            Category = UnitComposition.HasValue ? (cmdData as FleetCmdData).GenerateCmdCategory(UnitComposition.Value) : FleetCategory.None;
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            FleetCmdData fData = data as FleetCmdData;
            Target = fData.Target;
            UnitFullSpeed = fData.UnitFullSpeed;
            UnitMaxTurnRate = fData.UnitMaxTurnRate;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            FleetCmdData fData = data as FleetCmdData;
            CurrentSpeed = fData.CurrentSpeed;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

