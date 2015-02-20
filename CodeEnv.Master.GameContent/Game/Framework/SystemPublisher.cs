// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemPublisher.cs
// Report and LabelText Publisher for Systems.
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
    /// Report and LabelText Publisher for Systems.
    /// </summary>
    public class SystemPublisher : AItemPublisher<SystemReport, SystemData> {

        static SystemPublisher() {
            LabelTextFactory = new SystemLabelTextFactory();
        }

        private ISystemPublisherClient _systemItem;

        public SystemPublisher(SystemData data, ISystemPublisherClient systemItem)
            : base(data) {
            _systemItem = systemItem;
        }

        protected override bool IsCachedReportCurrent(Player player, out SystemReport cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) &&
                cachedReport.StarReport == _systemItem.GetStarReport(player) &&
                cachedReport.SettlementReport == _systemItem.GetSettlementReport(player) &&
                IsEqual(cachedReport.PlanetoidReports, _systemItem.GetPlanetoidReports(player));
        }

        protected override SystemReport GenerateReport(Player player) {
            var starReport = _systemItem.GetStarReport(player);
            var settlementReport = _systemItem.GetSettlementReport(player);
            var planetoidReports = _systemItem.GetPlanetoidReports(player);
            return new SystemReport(_data, player, starReport, settlementReport, planetoidReports);
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

