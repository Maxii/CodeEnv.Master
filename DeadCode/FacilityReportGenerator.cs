// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityReportGenerator.cs
// Report Generator for a Facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report Generator for a Facility.
    /// </summary>
    public class FacilityReportGenerator : AReportGenerator<FacilityData, FacilityReport> {

        static FacilityReportGenerator() {
            LabelFormatter = new FacilityLabelFormatter();
        }

        public FacilityReportGenerator(FacilityData data) : base(data) { }

        protected override FacilityReport GenerateReport(Player player, AIntel intel) {
            return new FacilityReport(_data, player, intel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

