// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarReportGenerator.cs
// Item ReportGenerator for a Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Item ReportGenerator for a Star.
    /// </summary>
    public class StarReportGenerator : AReportGenerator<StarItemData, StarReport> {

        static StarReportGenerator() {
            LabelFormatter = new StarLabelFormatter();
        }

        public StarReportGenerator(StarItemData data) : base(data) { }

        protected override StarReport GenerateReport(Player player, AIntel intel) {
            return new StarReport(_data, player, intel);
        }
        //protected override StarReport GenerateReport(Player player, IntelCoverage intelCoverage) {
        //    return new StarReport(_data, player, intelCoverage);
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

