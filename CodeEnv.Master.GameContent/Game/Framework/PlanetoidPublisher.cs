// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidPublisher.cs
// Report and LabelText Publisher for Planetoids.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Planetoids.
    /// </summary>
    public class PlanetoidPublisher : AIntelItemPublisher<PlanetoidReport, PlanetoidData> {

        static PlanetoidPublisher() {
            LabelTextFactory = new PlanetoidLabelTextFactory();
        }

        public PlanetoidPublisher(PlanetoidData data) : base(data) { }

        protected override PlanetoidReport GenerateReport(Player player) {
            return new PlanetoidReport(_data, player);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

