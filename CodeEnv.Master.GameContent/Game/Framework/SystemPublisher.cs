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

        /// <summary>
        /// Gets the SystemLabelText for this label for display to the Human Player.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <param name="starReport">The star report.</param>
        /// <param name="planetoidReports">The planetoid reports.</param>
        /// <returns></returns>
        //public SystemLabelText GetLabelText(LabelID labelID, StarReport starReport, PlanetoidReport[] planetoidReports) {
        //    SystemLabelText cachedLabelText;
        //    if (!IsCachedLabelTextCurrent(labelID, starReport, planetoidReports, out cachedLabelText)) {
        //        cachedLabelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer, starReport, planetoidReports), _data);
        //        CacheLabelText(labelID, cachedLabelText);
        //    }
        //    else {
        //        D.Log("{0} reusing cached {1} for Label {2}.", GetType().Name, typeof(SystemLabelText).Name, labelID.GetName());
        //    }
        //    return cachedLabelText;
        //}

        //private bool IsCachedLabelTextCurrent(LabelID labelID, StarReport starReport, PlanetoidReport[] planetoidReports, out SystemLabelText cachedLabelText) {
        //    SystemReport cachedReport;
        //    if (IsCachedReportCurrent(_gameMgr.HumanPlayer, starReport, planetoidReports, out cachedReport)) {
        //        if (TryGetCachedLabelText(labelID, out cachedLabelText)) {
        //            if (cachedLabelText.Report == cachedReport) {
        //                return true;
        //            }
        //        }
        //    }
        //    cachedLabelText = null;
        //    return false;
        //}

        //public bool TryUpdateLabelTextContent(LabelID labelID, LabelContentID contentID, StarReport starReport, PlanetoidReport[] planetoidReports, out IColoredTextList content) {
        //    var systemReport = GetReport(_gameMgr.HumanPlayer, starReport, planetoidReports);
        //    return LabelTextFactory.TryMakeInstance(labelID, contentID, systemReport, _data, out content);
        //}

        //private void CacheLabelText(LabelID labelID, SystemLabelText labelText) {
        //    _labelTextCache[labelID] = labelText;
        //}

        //private bool TryGetCachedLabelText(LabelID labelID, out SystemLabelText cachedLabelText) {
        //    if (_labelTextCache.TryGetValue(labelID, out cachedLabelText)) {
        //        return true;
        //    }
        //    cachedLabelText = null;
        //    return false;
        //}

        //private void CacheReport(Player player, SystemReport report) {
        //    _reportCache[player] = report;
        //}

        //private bool TryGetCachedReport(Player player, out SystemReport cachedReport) {
        //    if (_reportCache.TryGetValue(player, out cachedReport)) {
        //        return true;
        //    }
        //    return false;
        //}

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

