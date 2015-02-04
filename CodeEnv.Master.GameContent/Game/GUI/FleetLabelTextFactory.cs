﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetLabelTextFactory.cs
// LabelText factory for a fleet.
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
    /// LabelText factory for a fleet.
    /// </summary>
    public class FleetLabelTextFactory : ALabelTextFactory<FleetReport, FleetCmdData> {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.ParentName, "Parent: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Category, "Category: {0}"},
                {LabelContentID.Composition, "{0}"},
                {LabelContentID.Formation, "Formation: {0}"},
                {LabelContentID.CurrentCmdEffectiveness, "Cmd Eff: {0}"},
                {LabelContentID.UnitMaxWeaponsRange, "UnitMaxWeaponsRange: {0}"},
                {LabelContentID.UnitMaxSensorRange, "UnitMaxSensorRange: {0}"},
                {LabelContentID.UnitOffense, "UnitOffense: {0}"},
                {LabelContentID.UnitDefense, "UnitDefense: {0}"},
                {LabelContentID.UnitMaxHitPts, "UnitMaxHitPts: {0}"},
                {LabelContentID.UnitCurrentHitPts, "UnitCurrentHitPts: {0}"},
                {LabelContentID.UnitHealth, "UnitHealth: {0}"},

                {LabelContentID.Target, "Target: {0}"},
                {LabelContentID.TargetDistance, "TargetDistance: {0}"},
                {LabelContentID.CurrentSpeed, "Speed: {0}"},
                {LabelContentID.UnitFullSpeed, "UnitFullSpeed: {0}"},
                {LabelContentID.UnitMaxTurnRate, "UnitMaxTurnRate: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public FleetLabelTextFactory() : base() { }

        protected override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, FleetReport report, FleetCmdData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.ParentName:
                    content = !report.ParentName.IsNullOrEmpty() ? new ColoredTextList_String(report.ParentName) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_String(report.Owner.LeaderName) : content;
                    break;
                case LabelContentID.Category:
                    content = report.Category != FleetCategory.None ? new ColoredTextList<FleetCategory>(report.Category) : content;
                    break;
                case LabelContentID.Composition:
                    content = report.UnitComposition.HasValue ? new ColoredTextList<FleetUnitComposition>(report.UnitComposition.Value) : content;
                    break;
                case LabelContentID.Formation:
                    content = report.UnitFormation != Formation.None ? new ColoredTextList<Formation>(report.UnitFormation) : content;
                    break;
                case LabelContentID.CurrentCmdEffectiveness:
                    content = report.CurrentCmdEffectiveness.HasValue ? new ColoredTextList<float>(report.CurrentCmdEffectiveness.Value) : content;
                    break;
                case LabelContentID.UnitMaxWeaponsRange:
                    content = report.UnitMaxWeaponsRange.HasValue ? new ColoredTextList<float>(report.UnitMaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.UnitMaxSensorRange:
                    content = report.UnitMaxSensorRange.HasValue ? new ColoredTextList<float>(report.UnitMaxSensorRange.Value) : content;
                    break;
                case LabelContentID.UnitOffense:
                    content = report.UnitOffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitOffensiveStrength.Value) : content;
                    break;
                case LabelContentID.UnitDefense:
                    content = report.UnitDefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitDefensiveStrength.Value) : content;
                    break;
                case LabelContentID.UnitMaxHitPts:
                    content = report.UnitMaxHitPoints.HasValue ? new ColoredTextList<float>(report.UnitMaxHitPoints.Value) : content;
                    break;
                case LabelContentID.UnitCurrentHitPts:
                    content = report.UnitCurrentHitPoints.HasValue ? new ColoredTextList<float>(report.UnitCurrentHitPoints.Value) : content;
                    break;
                case LabelContentID.UnitHealth:
                    content = report.UnitHealth.HasValue ? new ColoredTextList<float>(report.UnitHealth.Value) : content;
                    break;
                case LabelContentID.Target:
                    content = report.Target != null ? new ColoredTextList_String(report.Target.DisplayName) : content;
                    break;
                case LabelContentID.TargetDistance:
                    content = report.Target != null ? new ColoredTextList_Distance(data.Position, report.Target.Position) : content;
                    break;
                case LabelContentID.CurrentSpeed:
                    content = report.CurrentSpeed.HasValue ? new ColoredTextList<float>(report.CurrentSpeed.Value) : content;
                    break;
                case LabelContentID.UnitFullSpeed:
                    content = report.UnitFullSpeed.HasValue ? new ColoredTextList<float>(report.UnitFullSpeed.Value) : content;
                    break;
                case LabelContentID.UnitMaxTurnRate:
                    content = report.UnitMaxTurnRate.HasValue ? new ColoredTextList<float>(report.UnitMaxTurnRate.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                case LabelContentID.IntelState:
                    content = MakeInstance(labelID, contentID, data);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyValue;
        }

        protected override IDictionary<LabelContentID, string> GetFormatLookup(LabelID labelID) {
            return _formatLookupByLabelID[labelID];
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

