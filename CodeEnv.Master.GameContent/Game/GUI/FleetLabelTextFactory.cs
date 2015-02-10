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

//#define DEBUG_LOG
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
    public class FleetLabelTextFactory : AIntelItemLabelTextFactory<FleetReport, FleetCmdItemData> {

        private static IDictionary<LabelID, IList<LabelContentID>> _includedContentLookup = new Dictionary<LabelID, IList<LabelContentID>>() {
            {LabelID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,
                LabelContentID.Composition,
                LabelContentID.Formation,
                LabelContentID.CurrentCmdEffectiveness,
                LabelContentID.UnitMaxWeaponsRange,
                LabelContentID.UnitMaxSensorRange,
                LabelContentID.UnitOffense,
                LabelContentID.UnitDefense,
                //LabelContentID.UnitMaxHitPts,
                //LabelContentID.UnitCurrentHitPts,
                LabelContentID.UnitHealth,

                LabelContentID.Target,
                LabelContentID.TargetDistance,
                LabelContentID.CurrentSpeed,
                LabelContentID.UnitFullSpeed,
                LabelContentID.UnitMaxTurnRate,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }}
        };

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _phraseOverrideLookup = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            {LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.UnitHealth, "UnitHealth: {0}, UnitMaxHP: {1}"}
            }}
        };


        public FleetLabelTextFactory() : base() { }

        public override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, FleetReport report, FleetCmdItemData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownContent : _emptyContent;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.ParentName:
                    content = !report.ParentName.IsNullOrEmpty() ? new ColoredTextList_String(report.ParentName) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_Owner(report.Owner) : content;
                    break;
                case LabelContentID.Category:
                    content = report.Category != FleetCategory.None ? new ColoredTextList_String(report.Category.GetName()) : content;
                    break;
                case LabelContentID.Composition:
                    content = report.UnitComposition != null ? new ColoredTextList_String(report.UnitComposition.ToString()) : content;
                    break;
                case LabelContentID.Formation:
                    content = report.UnitFormation != Formation.None ? new ColoredTextList_String(report.UnitFormation.GetName()) : content;
                    break;
                case LabelContentID.CurrentCmdEffectiveness:
                    content = report.CurrentCmdEffectiveness.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentCmdEffectiveness.Value) : content;
                    break;
                case LabelContentID.UnitMaxWeaponsRange:
                    content = report.UnitMaxWeaponsRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitMaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.UnitMaxSensorRange:
                    content = report.UnitMaxSensorRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitMaxSensorRange.Value) : content;
                    break;
                case LabelContentID.UnitOffense:
                    content = report.UnitOffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitOffensiveStrength.Value) : content;
                    break;
                case LabelContentID.UnitDefense:
                    content = report.UnitDefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.UnitDefensiveStrength.Value) : content;
                    break;
                //case LabelContentID.UnitMaxHitPts:
                //    content = report.UnitMaxHitPoints.HasValue ? new ColoredTextList<float>(format, report.UnitMaxHitPoints.Value) : content;
                //    break;
                //case LabelContentID.UnitCurrentHitPts:
                //    content = report.UnitCurrentHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitCurrentHitPoints.Value) : content;
                //    break;
                case LabelContentID.UnitHealth:
                    content = new ColoredTextList_Health(report.UnitHealth, report.UnitMaxHitPoints);
                    //content = report.UnitHealth.HasValue ? new ColoredTextList<float>(format, report.UnitHealth.Value) : content;
                    break;
                case LabelContentID.Target:
                    content = report.Target != null ? new ColoredTextList_String(report.Target.DisplayName) : content;
                    break;
                case LabelContentID.TargetDistance:
                    content = report.Target != null ? new ColoredTextList_Distance(data.Position, report.Target.Position) : content;
                    break;
                case LabelContentID.CurrentSpeed:
                    content = report.CurrentSpeed.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentSpeed.Value) : content;
                    break;
                case LabelContentID.UnitFullSpeed:
                    content = report.UnitFullSpeed.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitFullSpeed.Value) : content;
                    break;
                case LabelContentID.UnitMaxTurnRate:
                    content = report.UnitMaxTurnRate.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.UnitMaxTurnRate.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                case LabelContentID.IntelState:
                    content = new ColoredTextList_Intel(data.HumanPlayerIntel);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyContent;
        }

        protected override IEnumerable<LabelContentID> GetIncludedContentIDs(LabelID labelID) {
            return _includedContentLookup[labelID];
        }

        protected override bool TryGetOverridePhrase(LabelID labelID, LabelContentID contentID, out string overridePhrase) {
            if (_phraseOverrideLookup == null) {
                overridePhrase = null;
                return false;
            }
            return _phraseOverrideLookup[labelID].TryGetValue(contentID, out overridePhrase);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}
