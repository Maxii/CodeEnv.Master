// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidLabelTextFactory.cs
// LabelText factory for Planetoids.
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
    /// LabelText factory for Planetoids.
    /// </summary>
    public class PlanetoidLabelTextFactory : AIntelItemLabelTextFactory<PlanetoidReport, PlanetoidData> {

        private static IDictionary<LabelID, IList<LabelContentID>> _includedContentLookup = new Dictionary<LabelID, IList<LabelContentID>>() {
            {LabelID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,
                LabelContentID.Capacity,
                LabelContentID.Resources,
                LabelContentID.Specials,

                //LabelContentID.MaxHitPoints,
                //LabelContentID.CurrentHitPoints,
                LabelContentID.Health,
                LabelContentID.Defense,
                LabelContentID.Mass,
                LabelContentID.OrbitalSpeed,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }}
        };

#pragma warning disable 0649
        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _phraseOverrideLookup;
#pragma warning restore 0649


        public PlanetoidLabelTextFactory() : base() { }

        public override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, PlanetoidReport report, PlanetoidData data, out IColoredTextList content) {
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
                    content = report.Category != PlanetoidCategory.None ? new ColoredTextList_String(report.Category.GetName()) : content;
                    break;
                case LabelContentID.Capacity:
                    content = report.Capacity.HasValue ? new ColoredTextList<int>(GetFormat(contentID), report.Capacity.Value) : content;
                    break;
                case LabelContentID.Resources:
                    content = report.Resources.HasValue ? new ColoredTextList<OpeYield>(report.Resources.Value) : content;
                    break;
                case LabelContentID.Specials:
                    content = report.SpecialResources.HasValue ? new ColoredTextList<XYield>(report.SpecialResources.Value) : content;
                    break;
                //case LabelContentID.MaxHitPoints:
                //    content = report.MaxHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.MaxHitPoints.Value) : content;
                //    break;
                //case LabelContentID.CurrentHitPoints:
                //    content = report.CurrentHitPoints.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.CurrentHitPoints.Value) : content;
                //    break;
                case LabelContentID.Health:
                    content = new ColoredTextList_Health(report.Health, report.MaxHitPoints);
                    break;
                case LabelContentID.Defense:
                    content = report.DefensiveStrength.HasValue ? new ColoredTextList<CombatStrength>(report.DefensiveStrength.Value) : content;
                    break;
                case LabelContentID.Mass:
                    content = report.Mass.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.Mass.Value) : content;
                    break;
                case LabelContentID.OrbitalSpeed:
                    content = report.OrbitalSpeed.HasValue ? new ColoredTextList<float>(GetFormat(contentID), report.OrbitalSpeed.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                case LabelContentID.IntelState:
                    content = new ColoredTextList_Intel(data.GetHumanPlayerIntelCopy());
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

