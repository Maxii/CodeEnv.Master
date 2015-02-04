// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasePublisher.cs
// Report and LabelText Publisher for Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Starbases.
    /// </summary>
    public class StarbasePublisher : ACmdPublisher<StarbaseReport, StarbaseCmdData, FacilityReport> {

        static StarbasePublisher() {
            LabelTextFactory = new StarbaseLabelTextFactory();
        }

        public StarbasePublisher(StarbaseCmdData data) : base(data) { }

        protected override StarbaseReport GenerateReport(Player player, FacilityReport[] elementReports) {
            return new StarbaseReport(_data, player, elementReports);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

