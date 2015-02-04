// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemLabelFormatter.cs
// Label Formatter for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Label Formatter for Systems.
    /// </summary>
    public class SystemLabelFormatter : ALabelFormatter<SystemReport> {

        private static IDictionary<LabelID, IDictionary<LabelLineID, string>> _labelLookup = new Dictionary<LabelID, IDictionary<LabelLineID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelLineID, string>() {
                {LabelLineID.Name, "Name: {0}"},
                {LabelLineID.SectorIndex, "Sector: {0}"},
                {LabelLineID.Owner, "Owner: {0}"},
                {LabelLineID.Capacity, "Capacity: {0}"},
                {LabelLineID.Resources, "Resources: {0}"},
                {LabelLineID.Specials, "[800080]Specials:[-] {0}"}
            }}
            // TODO more LabelIDs
        };

        public SystemLabelFormatter() { }

        protected override IDictionary<LabelLineID, string> GetLabelLineLookup(LabelID labelID) {
            return _labelLookup[labelID];
        }

        protected override bool TryGetFormattedLine(LabelLineID lineID, out string formattedLine) {
            IColoredTextList content = IncludeUnknown ? _unknownValue : _emptyValue;
            switch (lineID) {
                case LabelLineID.Name:
                    content = Report.Name != null ? new ColoredTextList_String(Report.Name) : content;
                    break;
                case LabelLineID.SectorIndex:
                    content = new ColoredTextList<Index3D>(Report.SectorIndex);
                    break;
                case LabelLineID.Owner:
                    content = Report.Owner != null ? new ColoredTextList_String(Report.Owner.LeaderName) : content;
                    break;
                case LabelLineID.Capacity:
                    content = Report.Capacity.HasValue ? new ColoredTextList<int>(Report.Capacity.Value) : content;
                    break;
                case LabelLineID.Resources:
                    content = Report.Resources.HasValue ? new ColoredTextList<OpeYield>(Report.Resources.Value) : content;
                    break;
                case LabelLineID.Specials:
                    content = Report.SpecialResources.HasValue ? new ColoredTextList<XYield>(Report.SpecialResources.Value) : content;
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(lineID));
            }
            formattedLine = _labelLineLookup[lineID].Inject(content.TextElements);
            return content != _emptyValue;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

