// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasePublisher.cs
// Report and HudContent Publisher for Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Starbases.
    /// </summary>
    public class StarbasePublisher : ACmdPublisher<StarbaseReport, StarbaseCmdData> {

        public override ColoredStringBuilder ItemHudText {
            get { return StarbaseDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IStarbaseCmdItem _item;

        public StarbasePublisher(StarbaseCmdData data, IStarbaseCmdItem item)
            : base(data) {
            _item = item;
        }

        protected override StarbaseReport GenerateReport(Player player) {
            return new StarbaseReport(_data, player, _item);
        }

        protected override bool IsCachedReportCurrent(Player player, out StarbaseReport cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) && IsEqual(cachedReport.ElementReports, _item.GetElementReports(player));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

