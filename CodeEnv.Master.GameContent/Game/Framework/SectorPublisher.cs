// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorPublisher.cs
// Report and HudContent Publisher for Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Sectors.
    /// </summary>
    public class SectorPublisher : AItemPublisher<SectorReport, SectorData> {

        public override ColoredStringBuilder ItemHudText {
            get { return SectorDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private ISector_Ltd _item;

        public SectorPublisher(SectorData data, ISector_Ltd item) : base(data) { _item = item; }

        protected override SectorReport MakeReportInstance(Player player) {
            return new SectorReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

