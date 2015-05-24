// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementReport.cs
// Immutable report for SettlementCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SettlementCmdItems.
    /// </summary>
    public class SettlementReport : ACmdReport {

        public SettlementCategory Category { get; private set; }

        public BaseComposition UnitComposition { get; private set; }

        public int? Population { get; private set; }

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public float? Approval { get; private set; }

        public FacilityReport[] ElementReports { get; private set; }

        public SettlementReport(SettlementCmdData cmdData, Player player, ISettlementCmdItem item)
            : base(cmdData, player, item) {
            ElementReports = item.GetElementReports(player);
            AssignValuesFromElementReports(cmdData);
        }

        private void AssignValuesFromElementReports(SettlementCmdData cmdData) {
            var knownElementCategories = ElementReports.Select(r => r.Category).Where(cat => cat != default(FacilityCategory));
            if (knownElementCategories.Any()) { // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                UnitComposition = new BaseComposition(knownElementCategories);
            }
            Category = UnitComposition != null ? cmdData.GenerateCmdCategory(UnitComposition) : SettlementCategory.None;
            AssignValuesFrom(ElementReports);
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var sData = data as SettlementCmdData;
            Capacity = sData.Capacity;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            var sData = data as SettlementCmdData;
            Population = sData.Population;
            Resources = sData.Resources;
            Approval = sData.Approval;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

