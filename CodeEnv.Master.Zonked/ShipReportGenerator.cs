// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipReportGenerator.cs
// Report Generator for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report Generator for Ships.
    /// </summary>
    public class ShipReportGenerator : AReportGenerator<ShipData, ShipReport> {

        static ShipReportGenerator() {
            LabelFormatter = new ShipLabelFormatter();
        }

        public ShipReportGenerator(ShipData data) : base(data) { }

        protected override ShipReport GenerateReport(Player player, AIntel intel) {
            return new ShipReport(_data, player, intel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

