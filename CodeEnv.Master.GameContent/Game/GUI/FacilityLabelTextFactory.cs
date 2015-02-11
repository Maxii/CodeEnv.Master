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
    public class FacilityLabelTextFactory : AIntelItemLabelTextFactory<FacilityReport, FacilityData> {

        private static IDictionary<LabelID, IList<LabelContentID>> _includedContentLookup = new Dictionary<LabelID, IList<LabelContentID>>() {
            {LabelID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,
                //LabelContentID.MaxHitPoints,
                //LabelContentID.CurrentHitPoints,
                LabelContentID.Health,
                LabelContentID.Defense,
                LabelContentID.Mass,
                LabelContentID.MaxWeaponsRange,
                LabelContentID.MaxSensorRange,
                LabelContentID.Offense,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }}
        };

#pragma warning disable 0649
        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _phraseOverrideLookup;
#pragma warning restore 0649

        public FacilityLabelTextFactory() : base() { }

        public override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, FacilityReport report, FacilityData data, out IColoredTextList content) {
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
                    content = report.Category != FacilityCategory.None ? new ColoredTextList_String(report.Category.GetName()) : content;
                    break;
                //case LabelContentID.MaxHitPoints:
                //    content = report.MaxHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxHitPoints.Value) : content;
                //    break;
                //case LabelContentID.CurrentHitPoints:
                //    content = report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentHitPoints.Value) : content;
                //    break;
                case LabelContentID.Health:
                    content = new ColoredTextList_Health(report.Health, report.MaxHitPoints);
                    //content = report.Health.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Health.Value) : content;
                    break;
                case LabelContentID.Defense:
                    content = report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.DefensiveStrength.Value) : content;
                    break;
                case LabelContentID.Mass:
                    content = report.Mass.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Mass.Value) : content;
                    break;
                case LabelContentID.MaxWeaponsRange:
                    content = report.MaxWeaponsRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxWeaponsRange.Value) : content;
                    break;
                case LabelContentID.MaxSensorRange:
                    content = report.MaxSensorRange.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxSensorRange.Value) : content;
                    break;
                case LabelContentID.Offense:
                    content = report.OffensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.OffensiveStrength.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                case LabelContentID.IntelState:
                    content = new ColoredTextList_Intel(data.GetHumanPlayerIntel());
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

