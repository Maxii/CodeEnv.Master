// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ReportGenerator.cs
// Abstract generic base class for Item Data ReportGenerators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic base class for Item Data ReportGenerators.
    /// </summary>
    /// <typeparam name="DataType">The type of data.</typeparam>
    /// <typeparam name="ReportType">The type of report.</typeparam>
    public abstract class AReportGenerator<DataType, ReportType>
        where DataType : AItemData
        //where ReportType : class {
        where ReportType : AIntelItemReport {

        protected static ALabelFormatter<ReportType> LabelFormatter { private get; set; }

        protected DataType _data;
        private StringBuilder _stringBuilder;
        private IGameManager _gameMgr;
        private ReportType _report;
        private DisplayTargetID _currentTextLabelID;

        public AReportGenerator(DataType data) {
            _data = data;
            _gameMgr = References.GameManager;
            _stringBuilder = new StringBuilder();
        }

        public ReportType GetHumanPlayerReport(AIntel intel) {
            return GetReport(_gameMgr.UserPlayer, intel);
        }

        public ReportType GetReport(Player player, AIntel intel) {
            var intelCoverage = intel.CurrentCoverage;
            if (!IsReportCurrent(player, intelCoverage)) {
                D.Log("{0} generating new {1} for Player {2}, IntelCoverage {3}.", GetType().Name, typeof(ReportType).Name, player.LeaderName, intelCoverage.GetName());
                _report = GenerateReport(player, intel);
                _data.AcceptChanges();
            }
            return _report;
        }
        //public ReportType GetReport(Player player, IntelCoverage intelCoverage) {
        //    if (!IsReportCurrent(player, intelCoverage)) {
        //        D.Log("Generating new {0} for Player {1}, IntelCoverage {2}.", typeof(ReportType).Name, player.LeaderName, intelCoverage.GetName());
        //        _report = GenerateReport(player, intelCoverage);
        //        _data.AcceptChanges();
        //    }
        //    return _report;
        //}

        private bool IsReportCurrent(Player player, IntelCoverage intelCoverage) {
            return _report != null && _report.Player == player && _report.IntelCoverage == intelCoverage && !_data.IsChanged;
        }

        protected abstract ReportType GenerateReport(Player player, AIntel intel);
        //protected abstract ReportType GenerateReport(Player player, IntelCoverage intelCoverage);

        public string GetCursorHudText(AIntel intel, bool includeUnknown = true) {
            return GetText(DisplayTargetID.CursorHud, _gameMgr.UserPlayer, intel, includeUnknown);
        }
        //public string GetCursorHudText(IntelCoverage intelCoverage, bool includeUnknown = true) {
        //    return GetText(LabelID.CursorHud, _gameMgr.HumanPlayer, intelCoverage, includeUnknown);
        //}


        public string GetText(DisplayTargetID displayTgtID, Player player, AIntel intel, bool includeUnknown) {
            var intelCoverage = intel.CurrentCoverage;
            if (!IsTextCurrent(displayTgtID, player, intelCoverage, includeUnknown)) {
                D.Log("{0} generating new text for Label {1}, Player {2}, IntelCoverage {3}.", GetType().Name, displayTgtID.GetName(), player.LeaderName, intelCoverage.GetName());
                GenerateText(displayTgtID, player, intel, includeUnknown);
            }
            return _stringBuilder.ToString();
        }
        //public string GetText(LabelID displayTgtID, Player player, IntelCoverage intelCoverage, bool includeUnknown) {
        //    if (!IsTextCurrent(displayTgtID, player, intelCoverage, includeUnknown)) {
        //        D.Log("Generating new text for Label {0}, Player {1}, IntelCoverage {2}.", displayTgtID.GetName(), player.LeaderName, intelCoverage.GetName());
        //        GenerateText(displayTgtID, player, intelCoverage, includeUnknown);
        //    }
        //    return _stringBuilder.ToString();
        //}

        private bool IsTextCurrent(DisplayTargetID displayTgtID, Player player, IntelCoverage intelCoverage, bool includeUnknown) {
            return displayTgtID == _currentTextLabelID && includeUnknown == LabelFormatter.IncludeUnknown && IsReportCurrent(player, intelCoverage);
        }

        private void GenerateText(DisplayTargetID displayTgtID, Player player, AIntel intel, bool includeUnknown) {
            _stringBuilder.Clear();
            _currentTextLabelID = displayTgtID;
            LabelFormatter.IncludeUnknown = includeUnknown;
            LabelFormatter.Report = GetReport(player, intel);
            var labelLines = LabelFormatter.GetLabelLines(displayTgtID);
            foreach (var line in labelLines) {
                _stringBuilder.AppendLine(line);
                // IMPROVE don't include a line break on the last line
            }
        }
        //private void GenerateText(LabelID displayTgtID, Player player, IntelCoverage intelCoverage, bool includeUnknown) {
        //    _stringBuilder.Clear();
        //    _currentTextLabelID = displayTgtID;
        //    LabelFormatter.IncludeUnknown = includeUnknown;
        //    LabelFormatter.DataReport = GetReport(player, intelCoverage);
        //    var labelLines = LabelFormatter.GetLabelLines(displayTgtID);
        //    foreach (var line in labelLines) {
        //        _stringBuilder.AppendLine(line);
        //        // IMPROVE don't include a line break on the last line
        //    }
        //}

    }
}

