// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemPublisher.cs
// Abstract generic class for Publishers that support Items with no PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for Publishers that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemPublisher<ReportType, DataType> : APublisher
        where ReportType : AItemReport
        where DataType : AItemData {

        protected static AItemLabelTextFactory<ReportType, DataType> LabelTextFactory { private get; set; }

        protected DataType _data;
        private IDictionary<Player, ReportType> _reportCache = new Dictionary<Player, ReportType>();

        public AItemPublisher(DataType data)
            : base() {
            _data = data;
        }

        public ReportType GetReport(Player player) {
            ReportType cachedReport;
            if (!IsCachedReportCurrent(player, out cachedReport)) {
                cachedReport = GenerateReport(player);
                CacheReport(player, cachedReport);
                D.Log("{0} generated and cached a new {1} for {2}.", GetType().Name, typeof(ReportType).Name, player.LeaderName);
                _data.AcceptChanges();
            }
            else {
                D.Log("{0} reusing cached {1} for {2}.", GetType().Name, typeof(ReportType).Name, player.LeaderName);
            }
            return cachedReport;
        }

        protected virtual bool IsCachedReportCurrent(Player player, out ReportType cachedReport) {
            return TryGetCachedReport(player, out cachedReport) && !_data.IsChanged;
        }

        protected void CacheReport(Player player, ReportType report) {
            _reportCache[player] = report;
        }

        protected bool TryGetCachedReport(Player player, out ReportType cachedReport) {
            if (_reportCache.TryGetValue(player, out cachedReport)) {
                return true;
            }
            return false;
        }

        protected abstract ReportType GenerateReport(Player player);

        public override ALabelText GetLabelText(LabelID labelID) {
            ALabelText cachedLabelText;
            if (!IsCachedLabelTextCurrent(labelID, out cachedLabelText)) {
                cachedLabelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer), _data);
                CacheLabelText(labelID, cachedLabelText);
                D.Log("{0} generated and cached a new {1} for Label {2}.", GetType().Name, cachedLabelText.GetType().Name, labelID.GetName());
            }
            else {
                D.Log("{0} reusing cached {1} for Label {2}.", GetType().Name, cachedLabelText.GetType().Name, labelID.GetName());
            }
            return cachedLabelText;
        }

        protected bool IsCachedLabelTextCurrent(LabelID labelID, out ALabelText cachedLabelText) {
            ReportType cachedReport;
            if (IsCachedReportCurrent(_gameMgr.HumanPlayer, out cachedReport)) {
                if (TryGetCachedLabelText(labelID, out cachedLabelText)) {
                    if (cachedLabelText.Report == cachedReport) {
                        return true;
                    }
                }
            }
            cachedLabelText = null;
            return false;
        }

        public override bool TryUpdateLabelTextContent(LabelID labelID, LabelContentID contentID, out IColoredTextList content) {
            return LabelTextFactory.TryMakeInstance(labelID, contentID, GetReport(_gameMgr.HumanPlayer), _data, out content);
        }

    }
}

