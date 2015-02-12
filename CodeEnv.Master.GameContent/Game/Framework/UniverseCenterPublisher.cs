// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterPublisher.cs
// Report and LabelText Publisher for the Universe Center.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for the Universe Center.
    /// </summary>
    public class UniverseCenterPublisher : AIntelItemPublisher<UniverseCenterReport, UniverseCenterData> {

        static UniverseCenterPublisher() {
            LabelTextFactory = new UniverseCenterLabelTextFactory();
        }

        public UniverseCenterPublisher(UniverseCenterData data) : base(data) { }

        protected override UniverseCenterReport GenerateReport(Player player) {
            return new UniverseCenterReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

