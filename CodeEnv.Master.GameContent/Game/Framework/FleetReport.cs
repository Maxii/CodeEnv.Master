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
    ///Immutable report for FleetCmdItems.
    /// </summary>
    public class FleetReport : ACmdReport {

        public FleetCategory Category { get; private set; }

        /// <summary>
        /// The Composition of the fleet this report is about. The fleet elements
        /// (ships) reported will be limited to those ships the Player requesting
        /// the report has knowledge of. Will never be null as the Player will 
        /// always know about the HQElement of the fleet, since he knows about
        /// the fleet itself.
        /// </summary>
        public FleetComposition UnitComposition { get; private set; }

        public INavigableTarget Target { get; private set; }

        public float? CurrentSpeed { get; private set; }

        public float? UnitFullSpeed { get; private set; }

        public float? UnitMaxTurnRate { get; private set; }

        public ShipReport[] ElementReports { get; private set; }

        public FleetReport(FleetCmdData cmdData, Player player, IFleetCmdItem item)
            : base(cmdData, player, item) {
            ElementReports = item.GetElementReports(player);
            AssignValuesFromElementReports(cmdData);
        }

        private void AssignValuesFromElementReports(FleetCmdData cmdData) {
            var knownElementCategories = ElementReports.Select(r => r.Category).Where(cat => cat != default(ShipHullCategory));
            if (knownElementCategories.Any()) { // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                UnitComposition = new FleetComposition(knownElementCategories);
            }
            Category = UnitComposition != null ? cmdData.GenerateCmdCategory(UnitComposition) : FleetCategory.None;
            AssignValuesFrom(ElementReports);
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            FleetCmdData fData = data as FleetCmdData;
            UnitFullSpeed = fData.UnitFullSpeedValue;
            UnitMaxTurnRate = fData.UnitMaxTurnRate;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            FleetCmdData fData = data as FleetCmdData;
            Target = fData.Target;
            CurrentSpeed = fData.CurrentSpeedValue;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

