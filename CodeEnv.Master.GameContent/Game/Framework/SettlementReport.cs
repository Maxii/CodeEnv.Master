// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementReport.cs
// Immutable report on a settlement reflecting a specific player's IntelCoverage of
// the settlement's command and its elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report on a settlement reflecting a specific player's IntelCoverage of
    /// the settlement's command and its elements.
    /// </summary>
    public class SettlementReport : ACmdReport {

        public SettlementCategory Category { get; private set; }

        public BaseUnitComposition? UnitComposition { get; private set; }

        public int? Population { get; private set; }

        public float? CapacityUsed { get; private set; }

        public OpeYield? ResourcesUsed { get; private set; }

        public XYield? SpecialResourcesUsed { get; private set; }

        public SettlementReport(SettlementCmdData cmdData, Player player, FacilityReport[] facilityReports)
            : base(cmdData, player, facilityReports) { }

        protected override void AssignValuesFrom(AElementItemReport[] elementReports, ACommandData cmdData) {
            base.AssignValuesFrom(elementReports, cmdData);
            var knownElementCategories = elementReports.Cast<FacilityReport>().Select(r => r.Category).Where(cat => cat != FacilityCategory.None);
            UnitComposition = new BaseUnitComposition(knownElementCategories);
            Category = UnitComposition.HasValue ? (cmdData as SettlementCmdData).GenerateCmdCategory(UnitComposition.Value) : SettlementCategory.None;
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var sData = data as SettlementCmdData;
            CapacityUsed = sData.CapacityUsed;
            ResourcesUsed = sData.ResourcesUsed;
            SpecialResourcesUsed = sData.SpecialResourcesUsed;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            var sData = data as SettlementCmdData;
            Population = sData.Population;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

