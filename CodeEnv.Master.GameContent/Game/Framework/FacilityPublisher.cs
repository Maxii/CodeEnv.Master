// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityPublisher.cs
// Report and LabelText Publisher for Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Facilities.
    /// </summary>
    public class FacilityPublisher : APublisher<FacilityReport, FacilityData> {

        static FacilityPublisher() {
            LabelTextFactory = new FacilityLabelTextFactory();
        }

        public FacilityPublisher(FacilityData data) : base(data) { }

        protected override FacilityReport GenerateReport(Player player) {
            return new FacilityReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

