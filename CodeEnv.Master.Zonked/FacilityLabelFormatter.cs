// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityLabelFormatter.cs
// Label Formatter for a Facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Label Formatter for a Facility.
    /// </summary>
    public class FacilityLabelFormatter : ALabelFormatter<FacilityReport> {

        private static IDictionary<DisplayTargetID, IDictionary<LabelLineID, string>> _labelLookup = new Dictionary<DisplayTargetID, IDictionary<LabelLineID, string>>() {
            { DisplayTargetID.CursorHud, new Dictionary<LabelLineID, string>() {
                {LabelLineID.IntelCoverage, "IntelCoverage: {0}"},
                {LabelLineID.Name, "Name: {0}"},
                {LabelLineID.ParentName, "ParentName: {0}"},
                {LabelLineID.Owner, "Owner: {0}"},
                {LabelLineID.Category, "Category: {0}"},
                {LabelLineID.MaxHitPoints, "MaxHitPts: {0}"},
                {LabelLineID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {LabelLineID.Health, "Health: {0}"},
                {LabelLineID.Defense, "Defense: {0}"},
                {LabelLineID.Mass, "Mass: {0}"},
                {LabelLineID.MaxWeaponsRange, "MaxWeaponsRange: {0}"},
                {LabelLineID.MaxSensorRange, "MaxSensorRange: {0}"},
                {LabelLineID.Offense, "Offense: {0}"}
            }}
            // TODO more LabelIDs
        };

        public FacilityLabelFormatter() { }

        protected override IDictionary<LabelLineID, string> GetLabelLineLookup(DisplayTargetID displayTgtID) {
            return _labelLookup[displayTgtID];
        }

        protected override bool TryGetFormattedLine(LabelLineID lineID, out string formattedLine) {
            IColoredTextList content = IncludeUnknown ? _unknownValue : _emptyValue;
            switch (lineID) {
                case LabelLineID.IntelCoverage:
                    content = new ColoredTextList_String(Report.IntelCoverage.GetName());
                    break;
                case LabelLineID.Name:
                    content = Report.Name != null ? new ColoredTextList_String(Report.Name) : content;
                    break;
                case LabelLineID.ParentName:
                    content = Report.ParentName != null ? new ColoredTextList_String(Report.ParentName) : content;
                    break;
                case LabelLineID.Owner:
                    content = Report.Owner != null ? new ColoredTextList_String(Report.Owner.LeaderName) : content;
                    break;
                case LabelLineID.Category:
                    content = Report.Category != FacilityCategory.None ? new ColoredTextList<FacilityCategory>(Report.Category) : content;
                    break;
                case LabelLineID.MaxHitPoints:
                    content = Report.MaxHitPoints.HasValue ? new ColoredTextList<float>(Report.MaxHitPoints.Value) : content;
                    break;
                case LabelLineID.CurrentHitPoints:
                    content = Report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(Report.CurrentHitPoints.Value) : content;
                    break;
                case LabelLineID.Health:
                    content = Report.Health.HasValue ? new ColoredTextList<float>(Report.Health.Value) : content;
                    break;
                case LabelLineID.Defense:
                    content = Report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(Report.DefensiveStrength.Value) : content;
                    break;
                case LabelLineID.Mass:
                    content = Report.Mass.HasValue ? new ColoredTextList<float>(Report.Mass.Value) : content;
                    break;
                case LabelLineID.MaxWeaponsRange:
                    content = Report.MaxWeaponsRange.HasValue ? new ColoredTextList<float>(Report.MaxWeaponsRange.Value) : content;
                    break;
                case LabelLineID.MaxSensorRange:
                    content = Report.MaxSensorRange.HasValue ? new ColoredTextList<float>(Report.MaxSensorRange.Value) : content;
                    break;
                case LabelLineID.Offense:
                    content = Report.OffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(Report.OffensiveStrength.Value) : content;
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

