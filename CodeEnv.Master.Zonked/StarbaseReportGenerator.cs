// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseReportGenerator.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class StarbaseReportGenerator {

        private static ALabelFormatter<StarbaseReport> LabelFormatter { get; set; }

        static StarbaseReportGenerator() {
            LabelFormatter = new StarbaseLabelFormatter();
        }

        private StringBuilder _stringBuilder;
        private StarbaseCmdData _data;
        private IGameManager _gameMgr;
        private StarbaseReport _report;
        private DisplayTargetID _currentTextLabelID;
        private FacilityReport[] _elementReports;

        public StarbaseReportGenerator(StarbaseCmdData data) {
            _data = data;
            _gameMgr = References.GameManager;
            _stringBuilder = new StringBuilder();
        }

        public StarbaseReport GetReport(Player player, AIntel intel, FacilityReport[] elementReports) {
            if (!IsReportCurrent(player, intel.CurrentCoverage, elementReports)) {
                D.Log("{0} generating new {1} for Player {2}.", GetType().Name, typeof(StarbaseReport).Name, player.LeaderName);
                _elementReports = elementReports;
                _report = GenerateReport(player, intel, elementReports);
                _data.AcceptChanges();
            }
            return _report;
        }

        private bool IsReportCurrent(Player player, IntelCoverage intelCoverage, FacilityReport[] elementReports) {
            return _report != null && _report.Player == player && _report.IntelCoverage == intelCoverage && !_data.IsChanged && !elementReports.Except(_elementReports).Any();
        }

        protected StarbaseReport GenerateReport(Player player, AIntel intel, FacilityReport[] elementReports) {
            return new StarbaseReport(_data, player, intel, elementReports);
        }

        public string GetCursorHudText(AIntel intel, FacilityReport[] elementReports, bool includeUnknown = true) {
            return GetText(DisplayTargetID.CursorHud, _gameMgr.UserPlayer, intel, elementReports, includeUnknown);
        }

        public string GetText(DisplayTargetID displayTgtID, Player player, AIntel intel, FacilityReport[] elementReports, bool includeUnknown) {
            if (!IsTextCurrent(displayTgtID, player, intel.CurrentCoverage, elementReports, includeUnknown)) {
                D.Log("{0} generating new text for Label {1}, Player {2}.", GetType().Name, displayTgtID.GetName(), player.LeaderName);
                GenerateText(displayTgtID, player, intel, elementReports, includeUnknown);
            }
            return _stringBuilder.ToString();
        }

        private bool IsTextCurrent(DisplayTargetID displayTgtID, Player player, IntelCoverage intelCoverage, FacilityReport[] elementReports, bool includeUnknown) {
            return displayTgtID == _currentTextLabelID && includeUnknown == LabelFormatter.IncludeUnknown && IsReportCurrent(player, intelCoverage, elementReports);
        }

        private void GenerateText(DisplayTargetID displayTgtID, Player player, AIntel intel, FacilityReport[] elementReports, bool includeUnknown) {
            _stringBuilder.Clear();
            _currentTextLabelID = displayTgtID;
            LabelFormatter.IncludeUnknown = includeUnknown;
            LabelFormatter.Report = GetReport(player, intel, elementReports);
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

