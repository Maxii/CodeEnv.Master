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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Sectors.
    /// </summary>
    public class SectorPublisher : AIntelItemPublisher<SectorReport, SectorData> {

        public override ColoredStringBuilder ItemHudText {
            get { return SectorDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public SectorPublisher(SectorData data) : base(data) { }

        protected override SectorReport MakeReportInstance(Player player) {
            return new SectorReport(_data, player);
        }

    }
}

