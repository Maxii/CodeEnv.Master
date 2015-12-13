// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidReportGenerator.cs
// Report Generator for a Planetoid.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report Generator for a Planetoid.
    /// </summary>
    public class PlanetoidReportGenerator : AReportGenerator<APlanetoidData, PlanetoidReport> {

        static PlanetoidReportGenerator() {
            LabelFormatter = new PlanetoidLabelFormatter();
        }

        public PlanetoidReportGenerator(APlanetoidData data) : base(data) { }

        protected override PlanetoidReport GenerateReport(Player player, AIntel intel) {
            return new PlanetoidReport(_data, player, intel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

