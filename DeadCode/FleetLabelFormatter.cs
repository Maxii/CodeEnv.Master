// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetLabelFormatter.cs
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
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class FleetLabelFormatter : ALabelFormatter<FleetReport> {

        private static IDictionary<DisplayTargetID, IDictionary<LabelLineID, string>> _labelLookup = new Dictionary<DisplayTargetID, IDictionary<LabelLineID, string>>() {
            { DisplayTargetID.CursorHud, new Dictionary<LabelLineID, string>() {
                {LabelLineID.Name, "Name: {0}"},
                {LabelLineID.ParentName, "Parent: {0}"},
                {LabelLineID.Owner, "Owner: {0}"},
                {LabelLineID.Category, "Category: {0}"},
                {LabelLineID.Composition, "{0}"},
                {LabelLineID.Formation, "Formation: {0}"},
                {LabelLineID.CurrentCmdEffectiveness, "Cmd Eff: {0}"},
                {LabelLineID.UnitMaxWeaponsRange, "UnitMaxWeaponsRange: {0}"},
                {LabelLineID.UnitMaxSensorRange, "UnitMaxSensorRange: {0}"},
                {LabelLineID.UnitOffense, "UnitOffense: {0}"},
                {LabelLineID.UnitDefense, "UnitDefense: {0}"},
                {LabelLineID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {LabelLineID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {LabelLineID.UnitHealth, "UnitHealth: {0}"},

                {LabelLineID.Target, "Target: {0}"},
                {LabelLineID.CurrentSpeed, "Speed: {0}"},
                {LabelLineID.UnitFullSpeed, "UnitFullSpeed: {0}"},
                {LabelLineID.UnitMaxTurnRate, "UnitMaxTurnRate: {0}"}
            }}
            //TODO more LabelIDs
        };

        public FleetLabelFormatter() { }

        protected override IDictionary<LabelLineID, string> GetLabelLineLookup(DisplayTargetID displayTgtID) {
            return _labelLookup[displayTgtID];
        }

        protected override bool TryGetFormattedLine(LabelLineID lineID, out string formattedLine) {
            IColoredTextList content = IncludeUnknown ? _unknownValue : _emptyValue;
            switch (lineID) {
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
                    content = Report.Category != FleetCategory.None ? new ColoredTextList<FleetCategory>(Report.Category) : content;
                    break;
                case LabelLineID.Composition:
                    content = Report.UnitComposition.HasValue ? new ColoredTextList<FleetUnitComposition>(Report.UnitComposition.Value) : content;
                    break;
                case LabelLineID.Formation:
                    content = Report.UnitFormation != Formation.None ? new ColoredTextList<Formation>(Report.UnitFormation) : content;
                    break;
                case LabelLineID.CurrentCmdEffectiveness:
                    content = Report.CurrentCmdEffectiveness.HasValue ? new ColoredTextList<float>(Report.CurrentCmdEffectiveness.Value) : content;
                    break;
                case LabelLineID.UnitMaxWeaponsRange:
                    content = Report.UnitMaxWeaponsRange.HasValue ? new ColoredTextList<float>(Report.UnitMaxWeaponsRange.Value) : content;
                    break;
                case LabelLineID.UnitMaxSensorRange:
                    content = Report.UnitMaxSensorRange.HasValue ? new ColoredTextList<float>(Report.UnitMaxSensorRange.Value) : content;
                    break;
                case LabelLineID.UnitOffense:
                    content = Report.UnitOffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(Report.UnitOffensiveStrength.Value) : content;
                    break;
                case LabelLineID.UnitDefense:
                    content = Report.UnitDefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(Report.UnitDefensiveStrength.Value) : content;
                    break;
                case LabelLineID.UnitMaxHitPts:
                    content = Report.UnitMaxHitPoints.HasValue ? new ColoredTextList<float>(Report.UnitMaxHitPoints.Value) : content;
                    break;
                case LabelLineID.UnitCurrentHitPts:
                    content = Report.UnitCurrentHitPoints.HasValue ? new ColoredTextList<float>(Report.UnitCurrentHitPoints.Value) : content;
                    break;
                case LabelLineID.UnitHealth:
                    content = Report.UnitHealth.HasValue ? new ColoredTextList<float>(Report.UnitHealth.Value) : content;
                    break;

                case LabelLineID.Target:
                    content = Report.Target != null ? new ColoredTextList_String(Report.Target.DisplayName) : content;
                    break;
                case LabelLineID.CurrentSpeed:
                    content = Report.CurrentSpeed.HasValue ? new ColoredTextList<float>(Report.CurrentSpeed.Value) : content;
                    break;
                case LabelLineID.UnitFullSpeed:
                    content = Report.UnitFullSpeed.HasValue ? new ColoredTextList<float>(Report.UnitFullSpeed.Value) : content;
                    break;
                case LabelLineID.UnitMaxTurnRate:
                    content = Report.UnitMaxTurnRate.HasValue ? new ColoredTextList<float>(Report.UnitMaxTurnRate.Value) : content;
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

