// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LabelText.cs
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
    public class LabelPublisher<DataReportType> : APropertyChangeTracking where DataReportType : struct {

        public static ILabelFormatter<DataReportType> LabelFormatter { private get; set; }

        private DataReportType _dataReport;
        public DataReportType DataReport {
            get { return _dataReport; }
            set { SetProperty<DataReportType>(ref _dataReport, value, "DataReport", OnDataReportChanged); }
        }

        private StringBuilder _stringBuilder;

        public LabelPublisher() {
            _stringBuilder = new StringBuilder();
        }

        private void OnDataReportChanged() {
            _stringBuilder.Clear();
            GenerateText();
        }

        private void GenerateText() {
            LabelFormatter.DataReport = DataReport;
            var lineIDs = LabelFormatter.GetLineIDs();
            foreach (var lineID in lineIDs) {
                string line;
                if (LabelFormatter.TryGetFormattedLine(lineID, out line)) {
                    _stringBuilder.AppendLine(line);
                    // IMPROVE don't include a line break on the last line
                }
            }
        }

        public string GetText() {
            return _stringBuilder.ToString();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

