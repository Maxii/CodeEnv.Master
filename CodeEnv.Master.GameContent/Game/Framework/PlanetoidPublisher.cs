// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidPublisher.cs
// Report and HudContent Publisher for Planetoids.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Planetoids.
    /// </summary>
    public class PlanetoidPublisher : AIntelItemPublisher<PlanetoidReport, PlanetoidData> {

        public override ColoredStringBuilder HudContent {
            get { return PlanetoidDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IPlanetoidItem _item;

        public PlanetoidPublisher(PlanetoidData data, IPlanetoidItem item) : base(data) { _item = item; }

        protected override PlanetoidReport GenerateReport(Player player) {
            return new PlanetoidReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

