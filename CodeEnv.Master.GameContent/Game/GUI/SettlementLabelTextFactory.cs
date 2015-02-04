// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementLabelTextFactory.cs
// LabelText factory for Settlements.
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
    /// LabelText factory for Settlements.
    /// </summary>
    public class SettlementLabelTextFactory : ALabelTextFactory<SettlementReport, SettlementCmdData> {

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
                {LabelContentID.Population, "Population: {0}"},
                {LabelContentID.CapacityUsed, "CapacityUsed: {0}"},
                {LabelContentID.ResourcesUsed, "ResourcesUsed: {0}"},
                {LabelContentID.SpecialsUsed, "SpecialsUsed: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public SettlementLabelTextFactory() : base() { }

        protected override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, SettlementReport report, SettlementCmdData data, out IColoredTextList content) {
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
                    content = report.Category != SettlementCategory.None ? new ColoredTextList<SettlementCategory>(report.Category) : content;
                    break;
                case LabelContentID.Composition:
                    content = report.UnitComposition.HasValue ? new ColoredTextList<BaseUnitComposition>(report.UnitComposition.Value) : content;
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
                case LabelContentID.Population:
                    content = report.Population.HasValue ? new ColoredTextList<int>(report.Population.Value) : content;
                    break;
                case LabelContentID.CapacityUsed:
                    content = report.CapacityUsed.HasValue ? new ColoredTextList<float>(report.CapacityUsed.Value) : content;
                    break;
                case LabelContentID.ResourcesUsed:
                    content = report.ResourcesUsed.HasValue ? new ColoredTextList<OpeYield>(report.ResourcesUsed.Value) : content;
                    break;
                case LabelContentID.SpecialsUsed:
                    content = report.SpecialResourcesUsed.HasValue ? new ColoredTextList<XYield>(report.SpecialResourcesUsed.Value) : content;
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

