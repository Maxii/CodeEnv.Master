// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetPublisher.cs
// Report and HudContent Publisher for Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Fleets.
    /// </summary>
    public class FleetPublisher : ACmdPublisher<FleetReport, FleetCmdData> {

        public override ColoredStringBuilder HudContent {
            get { return FleetDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IFleetCmdItem _item;

        public FleetPublisher(FleetCmdData data, IFleetCmdItem item)
            : base(data) {
            _item = item;
        }

        protected override FleetReport GenerateReport(Player player) {
            return new FleetReport(_data, player, _item);
        }

        protected override bool IsCachedReportCurrent(Player player, out FleetReport cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) && IsEqual(cachedReport.ElementReports, _item.GetElementReports(player));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

