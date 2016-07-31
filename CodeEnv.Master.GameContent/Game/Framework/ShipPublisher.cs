// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPublisher.cs
// Report and HudContent Publisher for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Ships.
    /// </summary>
    public class ShipPublisher : AIntelItemPublisher<ShipReport, ShipData> {

        public override ColoredStringBuilder ItemHudText {
            get { return ShipDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IShip_Ltd _item;

        public ShipPublisher(ShipData data, IShip_Ltd item)
            : base(data) {
            _item = item;
        }

        protected override ShipReport MakeReportInstance(Player player) {
            return new ShipReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

