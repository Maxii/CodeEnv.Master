// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemPublisher.cs
// Report and HudContent Publisher for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Systems.
    /// </summary>
    public class SystemPublisher : AItemPublisher<SystemReport, SystemData> {

        public override ColoredStringBuilder HudContent {
            get { return SystemDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private ISystemItem _item;

        public SystemPublisher(SystemData data, ISystemItem item)
            : base(data) {
            _item = item;
        }

        protected override bool IsCachedReportCurrent(Player player, out SystemReport cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) &&
                cachedReport.StarReport == _item.GetStarReport(player) &&
                cachedReport.SettlementReport == _item.GetSettlementReport(player) &&
                IsEqual(cachedReport.PlanetoidReports, _item.GetPlanetoidReports(player));
        }

        protected override SystemReport GenerateReport(Player player) {
            return new SystemReport(_data, player, _item);
        }

        private bool IsEqual(IEnumerable<PlanetoidReport> reportsA, IEnumerable<PlanetoidReport> reportsB) {
            var isEqual = reportsA.OrderBy(r => r.Name).SequenceEqual(reportsB.OrderBy(r => r.Name));
            string equalsPhrase = isEqual ? "equal" : "not equal";
            D.Log("{0} PlanetoidReports are {1}.", GetType().Name, equalsPhrase);
            return isEqual;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

