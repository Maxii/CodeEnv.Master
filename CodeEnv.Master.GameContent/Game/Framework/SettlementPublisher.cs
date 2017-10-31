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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Settlements.
    /// </summary>
    public class SettlementPublisher : ACmdPublisher<SettlementCmdReport, SettlementCmdData> {

        public override ColoredStringBuilder ItemHudText {
            get { return SettlementDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public SettlementPublisher(SettlementCmdData data) : base(data) { }

        protected override SettlementCmdReport MakeReportInstance(Player player) {
            return new SettlementCmdReport(_data, player);
        }

    }
}

