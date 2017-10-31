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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Fleets.
    /// </summary>
    public class FleetPublisher : ACmdPublisher<FleetCmdReport, FleetCmdData> {

        public override ColoredStringBuilder ItemHudText {
            get { return FleetDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public FleetPublisher(FleetCmdData data) : base(data) { }

        protected override FleetCmdReport MakeReportInstance(Player player) {
            return new FleetCmdReport(_data, player);
        }

    }
}

