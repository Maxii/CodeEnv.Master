// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetReportGenerator.cs
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
    public class FleetReportGenerator {

        private static ALabelFormatter<FleetReport> LabelFormatter { get; set; }

        static FleetReportGenerator() {
            LabelFormatter = new FleetLabelFormatter();
        }

        private StringBuilder _stringBuilder;
        private FleetCmdData _data;
        private IGameManager _gameMgr;
        private FleetReport _report;
        private DisplayTargetID _currentTextLabelID;
        private ShipReport[] _elementReports;

        public FleetReportGenerator(FleetCmdData data) {
            _data = data;
            _gameMgr = References.GameManager;
            _stringBuilder = new StringBuilder();
        }

        public FleetReport GetReport(Player player, AIntel intel, ShipReport[] elementReports) {
            if (!IsReportCurrent(player, intel.CurrentCoverage, elementReports)) {
                D.Log("{0} generating new {1} for Player {2}.", GetType().Name, typeof(FleetReport).Name, player.LeaderName);
                _elementReports = elementReports;
                _report = GenerateReport(player, intel, elementReports);
                _data.AcceptChanges();
            }
            return _report;
        }

        private bool IsReportCurrent(Player player, IntelCoverage intelCoverage, ShipReport[] elementReports) {
            return _report != null && _report.Player == player && _report.IntelCoverage == intelCoverage && !_data.IsChanged && !elementReports.Except(_elementReports).Any();
        }

        protected FleetReport GenerateReport(Player player, AIntel intel, ShipReport[] elementReports) {
            return new FleetReport(_data, player, intel, elementReports);
        }

        public string GetCursorHudText(AIntel intel, ShipReport[] elementReports, bool includeUnknown = true) {
            return GetText(DisplayTargetID.CursorHud, _gameMgr.UserPlayer, intel, elementReports, includeUnknown);
        }

        public string GetText(DisplayTargetID displayTgtID, Player player, AIntel intel, ShipReport[] elementReports, bool includeUnknown) {
            if (!IsTextCurrent(displayTgtID, player, intel.CurrentCoverage, elementReports, includeUnknown)) {
                D.Log("{0} generating new text for Label {1}, Player {2}.", GetType().Name, displayTgtID.GetName(), player.LeaderName);
                GenerateText(displayTgtID, player, intel, elementReports, includeUnknown);
            }
            return _stringBuilder.ToString();
        }

        private bool IsTextCurrent(DisplayTargetID displayTgtID, Player player, IntelCoverage intelCoverage, ShipReport[] elementReports, bool includeUnknown) {
            return displayTgtID == _currentTextLabelID && includeUnknown == LabelFormatter.IncludeUnknown && IsReportCurrent(player, intelCoverage, elementReports);
        }

        private void GenerateText(DisplayTargetID displayTgtID, Player player, AIntel intel, ShipReport[] elementReports, bool includeUnknown) {
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

