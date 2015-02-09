// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdPublisher.cs
// Abstract generic class for Report and LabelText CmdPublishers.
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
    /// Abstract generic class for Report and LabelText CmdPublishers.
    /// </summary>
    public abstract class ACmdPublisher<ReportType, DataType, ElementReportType> : ACmdPublisherBase
        where ReportType : ACmdReport
        where DataType : AUnitCmdItemData
        where ElementReportType : AElementItemReport {

        protected static ALabelTextFactory<ReportType, DataType> LabelTextFactory { private get; set; }

        protected DataType _data;
        private IDictionary<Player, ReportType> _reportCache = new Dictionary<Player, ReportType>();

        public ACmdPublisher(DataType data)
            : base() {
            _data = data;
        }

        public ReportType GetReport(Player player, ElementReportType[] elementReports) {
            var intelCoverage = _data.GetIntelCoverage(player);
            ReportType cachedReport;
            if (!IsCachedReportCurrent(player, intelCoverage, elementReports, out cachedReport)) {
                cachedReport = GenerateReport(player, elementReports);
                CacheReport(player, cachedReport);
                _data.AcceptChanges();
            }
            else {
                D.Log("{0} reusing cached {1} for Player {2}, IntelCoverage {3}.", GetType().Name, typeof(ReportType).Name, player.LeaderName, intelCoverage.GetName());
            }
            return cachedReport;
        }

        private bool IsCachedReportCurrent(Player player, IntelCoverage intelCoverage, ElementReportType[] elementReports, out ReportType cachedReport) {
            return TryGetCachedReport(player, out cachedReport) && !_data.IsChanged && cachedReport.IntelCoverage == intelCoverage
                && IsEqual(cachedReport.ElementReports, elementReports);
        }

        protected abstract ReportType GenerateReport(Player player, ElementReportType[] elementReports);

        public override LabelText GetLabelText(LabelID labelID, AElementItemReport[] elementReports) {
            var eReports = elementReports.Cast<ElementReportType>().ToArray();
            LabelText cachedLabelText;
            var intelCoverage = _data.HumanPlayerIntelCoverage;
            if (!IsCachedLabelTextCurrent(labelID, intelCoverage, eReports, out cachedLabelText)) {
                cachedLabelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer, eReports), _data);
                CacheLabelText(labelID, cachedLabelText);
            }
            else {
                D.Log("{0} reusing cached {1} for Label {2}, HumanIntelCoverage {3}.", GetType().Name, typeof(LabelText).Name, labelID.GetName(), intelCoverage.GetName());
            }
            return cachedLabelText;
        }

        public override bool TryUpdateLabelTextContent(LabelID labelID, LabelContentID contentID, AElementItemReport[] elementReports, out IColoredTextList content) {
            var eReports = elementReports.Cast<ElementReportType>().ToArray();
            return LabelTextFactory.TryMakeInstance(labelID, contentID, GetReport(_gameMgr.HumanPlayer, eReports), _data, out content);
        }

        protected bool IsCachedLabelTextCurrent(LabelID labelID, IntelCoverage intelCoverage, ElementReportType[] elementReports, out LabelText cachedLabelText) {
            ReportType cachedReport;
            if (IsCachedReportCurrent(_gameMgr.HumanPlayer, intelCoverage, elementReports, out cachedReport)) {
                if (TryGetCachedLabelText(labelID, out cachedLabelText)) {
                    if (cachedLabelText.Report == cachedReport) {
                        return true;
                    }
                }
            }
            cachedLabelText = null;
            return false;
        }

        private void CacheLabelText(LabelID labelID, LabelText labelText) {
            _labelTextCache[labelID] = labelText;
        }

        private bool TryGetCachedLabelText(LabelID labelID, out LabelText cachedLabelText) {
            return _labelTextCache.TryGetValue(labelID, out cachedLabelText);
        }

        private void CacheReport(Player player, ReportType report) {
            _reportCache[player] = report;
        }

        private bool TryGetCachedReport(Player player, out ReportType cachedReport) {
            if (_reportCache.TryGetValue(player, out cachedReport)) {
                return true;
            }
            return false;
        }

        protected bool IsEqual(IEnumerable<AItemReport> reportsA, IEnumerable<AItemReport> reportsB) {
            return reportsA.OrderBy(r => r.Name).SequenceEqual(reportsB.OrderBy(r => r.Name));
        }

    }
}

