// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarPublisher.cs
// Report and LabelText Publisher for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Stars.
    /// </summary>
    public class StarPublisher : AIntelItemPublisher<StarReport, StarData> {

        static StarPublisher() {
            LabelTextFactory = new StarLabelTextFactory();
        }

        public StarPublisher(StarData data) : base(data) { }

        protected override StarReport GenerateReport(Player player) {
            return new StarReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

