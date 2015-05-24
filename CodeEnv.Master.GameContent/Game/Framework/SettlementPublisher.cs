// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementPublisher.cs
// Report and HudContent Publisher for Settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Settlements.
    /// </summary>
    public class SettlementPublisher : ACmdPublisher<SettlementReport, SettlementCmdData> {

        public override ColoredStringBuilder HudContent {
            get { return SettlementDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private ISettlementCmdItem _item;

        public SettlementPublisher(SettlementCmdData data, ISettlementCmdItem item)
            : base(data) {
            _item = item;
        }

        protected override SettlementReport GenerateReport(Player player) {
            return new SettlementReport(_data, player, _item);
        }

        protected override bool IsCachedReportCurrent(Player player, out SettlementReport cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) && IsEqual(cachedReport.ElementReports, _item.GetElementReports(player));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

