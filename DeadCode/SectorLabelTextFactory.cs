// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorLabelTextFactory.cs
// LabelText factory for Sectors.
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
    /// LabelText factory for Sectors.
    /// </summary>
    [System.Obsolete]
    public class SectorLabelTextFactory : AItemLabelTextFactory<SectorReport, SectorData> {

        private static IDictionary<DisplayTargetID, IList<LabelContentID>> _includedContentLookup = new Dictionary<DisplayTargetID, IList<LabelContentID>>() {
            {DisplayTargetID.CursorHud, new List<LabelContentID>() {
                LabelContentID.Name,
                LabelContentID.Owner,
                LabelContentID.SectorIndex,
                LabelContentID.Density,

                LabelContentID.CameraDistance
            }}
        };

#pragma warning disable 0649
        private static IDictionary<DisplayTargetID, IDictionary<LabelContentID, string>> _phraseOverrideLookup;
#pragma warning restore 0649

        public SectorLabelTextFactory() : base() { }

        public override bool TryMakeInstance(DisplayTargetID displayTgtID, LabelContentID contentID, SectorReport report, SectorData data, out IColoredTextList content) {
            content = _includeUnknownLookup[displayTgtID] ? _unknownContent : _emptyContent;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_Owner(report.Owner) : content;
                    break;
                case LabelContentID.SectorIndex:
                    content = new ColoredTextList<Index3D>(report.SectorIndex);
                    break;
                case LabelContentID.Density:
                    content = new ColoredTextList<float>(GetFormat(contentID), report.Density);
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

