// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorPublisher.cs
// Report and LabelText Publisher for Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and LabelText Publisher for Sectors.
    /// </summary>
    public class SectorPublisher {

        private static SectorLabelTextFactory LabelTextFactory { get; set; }

        static SectorPublisher() {
            LabelTextFactory = new SectorLabelTextFactory();
        }

        private IDictionary<Player, SectorReport> _reportCache = new Dictionary<Player, SectorReport>();
        private IDictionary<LabelID, SectorLabelText> _labelTextCache = new Dictionary<LabelID, SectorLabelText>();
        private IGameManager _gameMgr;
        private SectorData _data;

        public SectorPublisher(SectorData data) {
            _data = data;
            _gameMgr = References.GameManager;
        }

        public SectorReport GetReport(Player player) {
            SectorReport cachedReport;
            if (!IsCachedReportCurrent(player, out cachedReport)) {
                cachedReport = GenerateReport(player);
                CacheReport(player, cachedReport);
                _data.AcceptChanges();
            }
            else {
                D.Log("{0} reusing cached {1} for Player {2}.", GetType().Name, typeof(SectorReport).Name, player.LeaderName);
            }
            return cachedReport;
        }

        private bool IsCachedReportCurrent(Player player, out SectorReport cachedReport) {
            return TryGetCachedReport(player, out cachedReport) && !_data.IsChanged;
        }

        private SectorReport GenerateReport(Player player) {
            return new SectorReport(_data, player);
        }

        /// <summary>
        /// Gets the SectorLabelText for this label for display to the Human Player.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <returns></returns>
        public SectorLabelText GetLabelText(LabelID labelID) {
            SectorLabelText cachedLabelText;
            if (!IsCachedLabelTextCurrent(labelID, out cachedLabelText)) {
                cachedLabelText = LabelTextFactory.MakeInstance(labelID, GetReport(_gameMgr.HumanPlayer), _data);
                CacheLabelText(labelID, cachedLabelText);
            }
            else {
                D.Log("{0} reusing cached {1} for Label {2}.", GetType().Name, typeof(SectorLabelText).Name, labelID.GetName());
            }
            return cachedLabelText;
        }

        private bool IsCachedLabelTextCurrent(LabelID labelID, out SectorLabelText cachedLabelText) {
            SectorReport cachedReport;
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

        public bool TryUpdateLabelTextContent(LabelID labelID, LabelContentID contentID, out IColoredTextList content) {
            var systemReport = GetReport(_gameMgr.HumanPlayer);
            return LabelTextFactory.TryMakeInstance(labelID, contentID, systemReport, _data, out content);
        }

        private void CacheLabelText(LabelID labelID, SectorLabelText labelText) {
            _labelTextCache[labelID] = labelText;
        }

        private bool TryGetCachedLabelText(LabelID labelID, out SectorLabelText cachedLabelText) {
            if (_labelTextCache.TryGetValue(labelID, out cachedLabelText)) {
                return true;
            }
            cachedLabelText = null;
            return false;
        }

        private void CacheReport(Player player, SectorReport report) {
            _reportCache[player] = report;
        }

        private bool TryGetCachedReport(Player player, out SectorReport cachedReport) {
            if (_reportCache.TryGetValue(player, out cachedReport)) {
                return true;
            }
            return false;
        }

    }
}

