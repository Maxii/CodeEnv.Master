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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Systems.
    /// </summary>
    public class SystemPublisher {

        private static SystemLabelTextFactory LabelTextFactory { get; set; }

        static SystemPublisher() {
            LabelTextFactory = new SystemLabelTextFactory();
        }

        private IDictionary<Player, SystemReport> _reportCache = new Dictionary<Player, SystemReport>();
        private IDictionary<LabelID, SystemLabelText> _labelTextCache = new Dictionary<LabelID, SystemLabelText>();
        private IGameManager _gameMgr;
        private SystemData _data;

        public SystemPublisher(SystemData data) {
            _data = data;
            _gameMgr = References.GameManager;
        }

        public IColoredTextList UpdateLabelTextContent(LabelID labelID, LabelContentID contentID) {
            return LabelTextFactory.MakeInstance(labelID, contentID, _data);
        }

        public SystemReport GetReport(Player player, StarReport starReport, PlanetoidReport[] planetoidReports) {
            SystemReport cachedReport;
            if (!IsCachedReportCurrent(player, starReport, planetoidReports, out cachedReport)) {
                D.Log("{0} generating new {1} for Player {2}.", GetType().Name, typeof(SystemReport).Name, player.LeaderName);
                cachedReport = GenerateReport(player, starReport, planetoidReports);
                CacheReport(player, cachedReport);
                _data.AcceptChanges();
            }
            return cachedReport;
        }

        private bool IsCachedReportCurrent(Player player, StarReport starReport, PlanetoidReport[] planetoidReports, out SystemReport cachedReport) {
            return TryGetCachedReport(player, out cachedReport) && !_data.IsChanged && cachedReport.StarReport == starReport
                && IsEqual(cachedReport.PlanetoidReports, planetoidReports);
        }

        private SystemReport GenerateReport(Player player, StarReport starReport, PlanetoidReport[] planetoidReports) {
            return new SystemReport(player, _data, starReport, planetoidReports);
        }

        /// <summary>
        /// Gets the SystemLabelText for this label for display to the Human Player.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <param name="starReport">The star report.</param>
        /// <param name="planetoidReports">The planetoid reports.</param>
        /// <returns></returns>
        public SystemLabelText GetLabelText(LabelID labelID, StarReport starReport, PlanetoidReport[] planetoidReports) {
            SystemLabelText cachedLabelText;
            if (!IsCachedLabelTextCurrent(labelID, starReport, planetoidReports, out cachedLabelText)) {
                D.Log("{0} generating new {1} for Label {2}.", GetType().Name, typeof(SystemLabelText).Name, labelID.GetName());
                cachedLabelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer, starReport, planetoidReports), _data);
                CacheLabelText(labelID, cachedLabelText);
            }
            return cachedLabelText;
        }

        private bool IsCachedLabelTextCurrent(LabelID labelID, StarReport starReport, PlanetoidReport[] planetoidReports, out SystemLabelText cachedLabelText) {
            SystemReport cachedReport;
            if (IsCachedReportCurrent(_gameMgr.HumanPlayer, starReport, planetoidReports, out cachedReport)) {
                if (TryGetCachedLabelText(labelID, out cachedLabelText)) {
                    if (cachedLabelText.Report == cachedReport) {
                        return true;
                    }
                }
            }
            cachedLabelText = null;
            return false;
        }

        private void CacheLabelText(LabelID labelID, SystemLabelText labelText) {
            _labelTextCache[labelID] = labelText;
        }

        private bool TryGetCachedLabelText(LabelID labelID, out SystemLabelText cachedLabelText) {
            if (_labelTextCache.TryGetValue(labelID, out cachedLabelText)) {
                return true;
            }
            cachedLabelText = null;
            return false;
        }

        private void CacheReport(Player player, SystemReport report) {
            _reportCache[player] = report;
        }

        private bool TryGetCachedReport(Player player, out SystemReport cachedReport) {
            if (_reportCache.TryGetValue(player, out cachedReport)) {
                return true;
            }
            return false;
        }

        private bool IsEqual(IEnumerable<AReport> reportsA, IEnumerable<AReport> reportsB) {
            return reportsA.OrderBy(r => r.Name).SequenceEqual(reportsB.OrderBy(r => r.Name));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

