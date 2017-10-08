// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemPublisher.cs
// Abstract generic class for Publishers that support Items with PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract generic class for Publishers that support Items with PlayerIntel.
    /// </summary>
    public abstract class AIntelItemPublisher<ReportType, DataType> : AItemPublisher<ReportType, DataType>
        where ReportType : AIntelItemReport
        where DataType : AIntelItemData {

        public AIntelItemPublisher(DataType data)
            : base(data) {
        }

        #region Report Cache Archive

        [System.Obsolete]
        protected override bool IsCachedReportCurrent(Player player, out ReportType cachedReport) {
            return base.IsCachedReportCurrent(player, out cachedReport) && cachedReport.IntelCoverage == _data.GetIntelCoverage(player);
        }

        #endregion

    }
}

