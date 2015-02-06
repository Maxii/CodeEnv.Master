// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityLabelTextFactory.cs
// LabelText factory for Facilities.
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
    /// LabelText factory for Facilities.
    /// </summary>
    public class FacilityLabelTextFactory : ALabelTextFactory<FacilityReport, FacilityData> {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.ParentName, "ParentName: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Category, "Category: {0}"},
                {LabelContentID.MaxHitPoints, "MaxHitPts: {0}"},
                {LabelContentID.CurrentHitPoints, "CurrentHitPts: {0}"},
                {LabelContentID.Health, "Health: {0}"},
                {LabelContentID.Defense, "Defense: {0}"},
                {LabelContentID.Mass, "Mass: {0}"},
                {LabelContentID.MaxWeaponsRange, "MaxWeaponsRange: {0}"},
                {LabelContentID.MaxSensorRange, "MaxSensorRange: {0}"},
                {LabelContentID.Offense, "Offense: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public FacilityLabelTextFactory() : base() { }

        public override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, FacilityReport report, FacilityData data, out IColoredTextList content) {
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
                    content = report.Category != FacilityCategory.None ? new ColoredTextList<FacilityCategory>(report.Category) : content;
                    break;
                case LabelContentID.MaxHitPoints:
                    content = report.MaxHitPoints.HasValue ? new ColoredTextList<float>(report.MaxHitPoints.Value) : content;
                    break;
                case LabelContentID.CurrentHitPoints:
                    content = report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(report.CurrentHitPoints.Value) : content;
                    break;
                case LabelContentID.Health:
                    content = report.Health.HasValue ? new ColoredTextList<float>(report.Health.Value) : content;
                    break;
                case LabelContentID.Defense:
                    content = report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.DefensiveStrength.Value) : content;
                    break;
                case LabelContentID.Mass:
                    content = report.Mass.HasValue ? new ColoredTextList<float>(report.Mass.Value) : content;
                    break;
                case LabelContentID.MaxWeaponsRange:
                    content = report.MaxWeaponsRange.HasValue ? new ColoredTextList<float>(report.MaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.MaxSensorRange:
                    content = report.MaxSensorRange.HasValue ? new ColoredTextList<float>(report.MaxSensorRange.Value) : content;
                    break;
                case LabelContentID.Offense:
                    content = report.OffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.OffensiveStrength.Value) : content;
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

