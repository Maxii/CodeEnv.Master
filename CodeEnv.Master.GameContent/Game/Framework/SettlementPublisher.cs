// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementPublisher.cs
// Report and LabelText Publisher for Settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Settlements.
    /// </summary>
    public class SettlementPublisher : ACmdPublisher<SettlementReport, SettlementCmdItemData, FacilityReport> {

        static SettlementPublisher() {
            LabelTextFactory = new SettlementLabelTextFactory();
        }

        public SettlementPublisher(SettlementCmdItemData data) : base(data) { }

        protected override SettlementReport GenerateReport(Player player, FacilityReport[] elementReports) {
            return new SettlementReport(_data, player, elementReports);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

