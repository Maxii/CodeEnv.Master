// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarLabelTextFactory.cs
// LabelText Factory for Stars.
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
    /// LabelText Factory for Stars.
    /// </summary>
    public class StarLabelTextFactory : AIntelItemLabelTextFactory<StarReport, StarItemData> {

        private static IDictionary<LabelID, IList<LabelContentID>> _includedContentLookup = new Dictionary<LabelID, IList<LabelContentID>>() {
            {LabelID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.ParentName,
                LabelContentID.Owner,
                LabelContentID.Category,

                LabelContentID.Capacity,
                LabelContentID.Resources,
                LabelContentID.Specials,

                LabelContentID.CameraDistance,
                LabelContentID.IntelState
            }}
        };

#pragma warning disable 0649
        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _phraseOverrideLookup;
#pragma warning restore 0649

        public StarLabelTextFactory() : base() { }

        public override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, StarReport report, StarItemData data, out IColoredTextList content) {
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
                    content = report.Category != StarCategory.None ? new ColoredTextList_String(report.Category.GetName()) : content;
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

