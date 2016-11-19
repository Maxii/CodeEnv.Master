// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityPublisher.cs
// Report and HudContent Publisher for Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Facilities.
    /// </summary>
    public class FacilityPublisher : AIntelItemPublisher<FacilityReport, FacilityData> {

        public override ColoredStringBuilder ItemHudText {
            get { return FacilityDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IFacility_Ltd _item;

        public FacilityPublisher(FacilityData data, IFacility_Ltd item)
            : base(data) {
            _item = item;
        }

        protected override FacilityReport MakeReportInstance(Player player) {
            return new FacilityReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

