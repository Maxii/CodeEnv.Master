// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetPublisher.cs
// Report and LabelText Publisher for Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Fleets.
    /// </summary>
    public class FleetPublisher : ACmdPublisher<FleetReport, FleetCmdItemData, ShipReport> {

        static FleetPublisher() {
            LabelTextFactory = new FleetLabelTextFactory();
        }

        public FleetPublisher(FleetCmdItemData data, ICmdPublisherClient<ShipReport> cmdItem)
            : base(data, cmdItem) {
        }

        protected override FleetReport GenerateReport(Player player) {
            return new FleetReport(_data, player, _cmdItem.GetElementReports(player));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

