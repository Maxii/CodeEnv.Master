// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APublisher.cs
// Abstract generic class for Report and LabelText Publishers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for Report and LabelText Publishers.
    /// </summary>
    public abstract class APublisher<ReportType, DataType> : APublisherBase
        where ReportType : AItemReport
        where DataType : AItemData {

        protected static ALabelTextFactory<ReportType, DataType> LabelTextFactory { private get; set; }

        protected DataType _data;
        private IDictionary<Player, ReportType> _reportCache = new Dictionary<Player, ReportType>();

        public APublisher(DataType data)
            : base() {
            _data = data;
        }

        public ReportType GetReport(Player player) {
            D.Assert(_data != null, "Data is null.");
            D.Assert(_data.GetPlayerIntel(player) != null);
            var intelCoverage = _data.GetPlayerIntel(player).CurrentCoverage;
            ReportType report;
            if (!IsCachedReportCurrent(player, intelCoverage, out report)) {
                report = GenerateReport(player);
                CacheReport(player, report);
                _data.AcceptChanges();
            }
            else {
                D.Log("{0} reusing cached {1} for Player {2}, IntelCoverage {3}.", GetType().Name, typeof(ReportType).Name, player.LeaderName, intelCoverage.GetName());
            }
            return report;
        }

        private bool IsCachedReportCurrent(Player player, IntelCoverage intelCoverage, out ReportType report) {
            return TryGetCachedReport(player, out report) && !_data.IsChanged && report.IntelCoverage == intelCoverage;
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

        protected abstract ReportType GenerateReport(Player player);

        public override LabelText GetLabelText(LabelID labelID) {
            LabelText labelText;
            var intelCoverage = _data.HumanPlayerIntelCoverage;
            if (!IsCachedLabelTextCurrent(labelID, intelCoverage, out labelText)) {
                D.Log("{0} generating new {1} for Label {2}, HumanIntelCoverage {3}.", GetType().Name, typeof(LabelText).Name, labelID.GetName(), intelCoverage.GetName());
                labelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer), _data);
            }
            return labelText;
        }

        public override IColoredTextList UpdateContent(LabelID labelID, LabelContentID contentID) {
            return LabelTextFactory.MakeInstance(labelID, contentID, _data);
        }

    }
}

