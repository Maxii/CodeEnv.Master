// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelFormatter.cs
// Abstract generic class for Label Formatters.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for Label Formatters.
    /// </summary>
    //public abstract class ALabelFormatter : ALabelFormatterBase {
    //public abstract class ALabelFormatter<ReportType> : ALabelFormatterBase where ReportType : AItemReport {
    public abstract class ALabelFormatter<ReportType> : ALabelFormatterBase where ReportType : class {

        public bool IncludeUnknown { get; set; }

        public ReportType Report { protected get; set; }

        protected IDictionary<LabelLineID, string> _labelLineLookup;

        public IList<string> GetLabelLines(DisplayTargetID displayTgtID) {
            _labelLineLookup = GetLabelLineLookup(displayTgtID);

            var formattedLines = new List<string>();
            string formattedLine;
            foreach (var lineID in _labelLineLookup.Keys) {
                if (TryGetFormattedLine(lineID, out formattedLine)) {
                    formattedLines.Add(formattedLine);
                }
            }
            return formattedLines;
        }

        /// <summary>
        /// Provides the lookup table for label lines, sourced from the derived class' labelIDLookup table.
        /// </summary>
        /// <param name="displayTgtID">The label identifier.</param>
        /// <returns></returns>
        protected abstract IDictionary<LabelLineID, string> GetLabelLineLookup(DisplayTargetID displayTgtID);

        /// <summary>
        /// Tries to return the formatted line associated with this lineID, returning <c>true</c> if
        /// the line should be included in the label. Lines that contain unknown content when 
        /// <c>includeUnknown</c> is not set should return false.
        /// </summary>
        /// <param name="lineID">The line identifier.</param>
        /// <param name="formattedLine">The formatted line.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected abstract bool TryGetFormattedLine(LabelLineID lineID, out string formattedLine);

    }
}

