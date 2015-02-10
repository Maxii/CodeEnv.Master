﻿// --------------------------------------------------------------------------------------------------------------------
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

        public float? CapacityUsed { get; private set; }

        public OpeYield? ResourcesUsed { get; private set; }

        public XYield? SpecialResourcesUsed { get; private set; }

        public SettlementReport(SettlementCmdItemData cmdData, Player player, FacilityReport[] facilityReports)
            : base(cmdData, player, facilityReports) { }

        protected override void AssignValuesFrom(AElementItemReport[] elementReports, AUnitCmdItemData cmdData) {
            base.AssignValuesFrom(elementReports, cmdData);
            var knownElementCategories = elementReports.Cast<FacilityReport>().Select(r => r.Category).Where(cat => cat != FacilityCategory.None);
            UnitComposition = new BaseComposition(knownElementCategories);
            Category = UnitComposition != null ? (cmdData as SettlementCmdItemData).GenerateCmdCategory(UnitComposition) : SettlementCategory.None;
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var sData = data as SettlementCmdItemData;
            CapacityUsed = sData.CapacityUsed;
            ResourcesUsed = sData.ResourcesUsed;
            SpecialResourcesUsed = sData.SpecialResourcesUsed;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            var sData = data as SettlementCmdItemData;
            Population = sData.Population;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

