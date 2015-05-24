// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemReportGenerator.cs
// Item ReportGenerator for a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Item ReportGenerator for a System.
    /// </summary>
    public class SystemReportGenerator {

        private static ALabelFormatter<SystemReport> LabelFormatter { get; set; }

        static SystemReportGenerator() {
            LabelFormatter = new SystemLabelFormatter();
        }

        private StringBuilder _stringBuilder;
        private SystemData _data;
        private IGameManager _gameMgr;
        private SystemReport _report;
        private DisplayTargetID _currentTextLabelID;
        private StarReport _starReport;
        private PlanetoidReport[] _planetoidReports;

        public SystemReportGenerator(SystemData data) {
            _data = data;
            _gameMgr = References.GameManager;
            _stringBuilder = new StringBuilder();
        }

        public SystemReport GetReport(Player player, StarReport starReport, PlanetoidReport[] planetoidReports) {
            if (!IsReportCurrent(player, starReport, planetoidReports)) {
                D.Log("{0} generating new {1} for Player {2}.", GetType().Name, typeof(SystemReport).Name, player.LeaderName);
                _starReport = starReport;
                _planetoidReports = planetoidReports;
                _report = GenerateReport(player, starReport, planetoidReports);
                _data.AcceptChanges();
            }
            return _report;
        }

        private bool IsReportCurrent(Player player, StarReport starReport, PlanetoidReport[] planetoidReports) {
            return _report != null && _report.Player == player && !_data.IsChanged && starReport == _starReport && !planetoidReports.Except(_planetoidReports).Any();
        }

        protected SystemReport GenerateReport(Player player, StarReport starReport, PlanetoidReport[] planetoidReports) {
            return new SystemReport(player, _data, starReport, planetoidReports);
        }

        public string GetCursorHudText(StarReport starReport, PlanetoidReport[] planetoidReports, bool includeUnknown = true) {
            return GetText(DisplayTargetID.CursorHud, _gameMgr.UserPlayer, starReport, planetoidReports, includeUnknown);
        }

        public string GetText(DisplayTargetID displayTgtID, Player player, StarReport starReport, PlanetoidReport[] planetoidReports, bool includeUnknown) {
            if (!IsTextCurrent(displayTgtID, player, starReport, planetoidReports, includeUnknown)) {
                D.Log("{0} generating new text for Label {1}, Player {2}.", GetType().Name, displayTgtID.GetName(), player.LeaderName);
                GenerateText(displayTgtID, player, starReport, planetoidReports, includeUnknown);
            }
            return _stringBuilder.ToString();
        }

        private bool IsTextCurrent(DisplayTargetID displayTgtID, Player player, StarReport starReport, PlanetoidReport[] planetoidReports, bool includeUnknown) {
            return displayTgtID == _currentTextLabelID && includeUnknown == LabelFormatter.IncludeUnknown && IsReportCurrent(player, starReport, planetoidReports);
        }

        private void GenerateText(DisplayTargetID displayTgtID, Player player, StarReport starReport, PlanetoidReport[] planetoidReports, bool includeUnknown) {
            _stringBuilder.Clear();
            _currentTextLabelID = displayTgtID;
            LabelFormatter.IncludeUnknown = includeUnknown;
            LabelFormatter.Report = GetReport(player, starReport, planetoidReports);
            var labelLines = LabelFormatter.GetLabelLines(displayTgtID);
            foreach (var line in labelLines) {
                _stringBuilder.AppendLine(line);
                // IMPROVE don't include a line break on the last line
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

