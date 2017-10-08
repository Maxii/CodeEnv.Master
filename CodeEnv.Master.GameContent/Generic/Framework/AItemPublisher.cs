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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for Publishers that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemPublisher<ReportType, DataType> : APublisher
        where ReportType : AItemReport
        where DataType : AItemData {

        protected bool ShowDebugLog { get { return _data.ShowDebugLog; } }

        protected DataType _data;


        public AItemPublisher(DataType data)
            : base() {
            _data = data;
        }

        public ReportType GetUserReport() { return GetReport(_gameMgr.UserPlayer); }

        public ReportType GetReport(Player player) {
            return MakeReportInstance(player);
        }
        protected abstract ReportType MakeReportInstance(Player player);

        #region Report Cache Archive

        // OPTIMIZE 9.30.17 Removed as I can't reliably keep data change tracking current. Once Data content stabilizes
        // I have the option of re-implementing this.

        [System.Obsolete]
        private IDictionary<Player, ReportType> _reportCache = new Dictionary<Player, ReportType>();

        //public ReportType GetReport(Player player) {
        //    ReportType cachedReport;
        //    if (!IsCachedReportCurrent(player, out cachedReport)) {
        //        cachedReport = MakeReportInstance(player);
        //        CacheReport(player, cachedReport);
        //        //D.Log(ShowDebugLog, "{0} generated and cached a new {1} for {2}.", GetType().Name, typeof(ReportType).Name, player.LeaderName);
        //        _data.AcceptChanges();
        //    }
        //    else {
        //        //D.Log(ShowDebugLog, "{0} reusing cached {1} for {2}.", GetType().Name, typeof(ReportType).Name, player.LeaderName);
        //    }
        //    return cachedReport;
        //}

        [System.Obsolete]
        protected virtual bool IsCachedReportCurrent(Player player, out ReportType cachedReport) {
            return TryGetCachedReport(player, out cachedReport) && !_data.IsChanged;
        }

        [System.Obsolete]
        protected void CacheReport(Player player, ReportType report) {
            _reportCache[player] = report;
        }

        [System.Obsolete]
        protected bool TryGetCachedReport(Player player, out ReportType cachedReport) {
            if (_reportCache.TryGetValue(player, out cachedReport)) {
                return true;
            }
            return false;
        }

        #endregion

    }
}

