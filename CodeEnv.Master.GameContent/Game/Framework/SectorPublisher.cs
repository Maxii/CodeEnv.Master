// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorPublisher.cs
// Report and LabelText Publisher for Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Sectors.
    /// </summary>
    public class SectorPublisher : AItemPublisher<SectorReport, SectorData> {

        static SectorPublisher() {
            LabelTextFactory = new SectorLabelTextFactory();
        }

        public SectorPublisher(SectorData data) : base(data) { }

        protected override SectorReport GenerateReport(Player player) {
            return new SectorReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

