// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPublisher.cs
// Report and LabelText Publisher for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Ships.
    /// </summary>
    public class ShipPublisher : APublisher<ShipReport, ShipItemData> {

        static ShipPublisher() {
            LabelTextFactory = new ShipLabelTextFactory();
        }

        public ShipPublisher(ShipItemData data) : base(data) { }

        protected override ShipReport GenerateReport(Player player) {
            return new ShipReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

