// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemLabelTextFactory.cs
// LabelText Factory for Systems.
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
    /// LabelText Factory for Systems.
    /// </summary>
    [System.Obsolete]
    public class SystemLabelTextFactory : AItemLabelTextFactory<SystemReport, SystemData> {

        private static IDictionary<DisplayTargetID, IList<LabelContentID>> _includedContentLookup = new Dictionary<DisplayTargetID, IList<LabelContentID>>() {
            {DisplayTargetID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.SectorIndex,
                LabelContentID.Owner,

                LabelContentID.Capacity,
                LabelContentID.Resources,

                LabelContentID.CameraDistance
            }},
            {DisplayTargetID.SystemsScreen, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.SectorIndex
            }}
        };

        //#pragma warning disable 0649
        private static IDictionary<DisplayTargetID, IDictionary<LabelContentID, string>> _phraseOverrideLookup = new Dictionary<DisplayTargetID, IDictionary<LabelContentID, string>>() {
            {DisplayTargetID.SystemsScreen, new Dictionary<LabelContentID, string>() {
                {LabelContentID.Name, "{0}"},
                {LabelContentID.SectorIndex, "{0}"}
            }}
        };
        //#pragma warning restore 0649

        public SystemLabelTextFactory() : base() { }

        public override bool TryMakeInstance(DisplayTargetID displayTgtID, LabelContentID contentID, SystemReport report, SystemData data, out IColoredTextList content) {
            content = _includeUnknownLookup[displayTgtID] ? _unknownContent : _emptyContent;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.SectorIndex:
                    content = new ColoredTextList<Index3D>(report.SectorIndex);
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_Owner(report.Owner) : content;
                    break;
                case LabelContentID.Capacity:
                    content = report.Capacity.HasValue ? new ColoredTextList<int>(GetFormat(contentID), report.Capacity.Value) : content;
                    break;
                case LabelContentID.Resources:
                    content = report.Resources.HasValue ? new ColoredTextList<ResourceYield>(report.Resources.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyContent;
        }

        protected override IEnumerable<LabelContentID> GetIncludedContentIDs(DisplayTargetID displayTgtID) {
            return _includedContentLookup[displayTgtID];
        }

        protected override bool TryGetOverridePhrase(DisplayTargetID displayTgtID, LabelContentID contentID, out string overridePhrase) {
            if (_phraseOverrideLookup != null && _phraseOverrideLookup.Keys.Contains(displayTgtID)) {
                return _phraseOverrideLookup[displayTgtID].TryGetValue(contentID, out overridePhrase);
            }
            overridePhrase = null;
            return false;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

