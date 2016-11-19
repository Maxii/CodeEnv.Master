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

        private ISettlementCmd_Ltd _item;

        public SettlementPublisher(SettlementCmdData data, ISettlementCmd_Ltd item)
            : base(data) {
            _item = item;
        }

        protected override SettlementCmdReport MakeReportInstance(Player player) {
            return new SettlementCmdReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

