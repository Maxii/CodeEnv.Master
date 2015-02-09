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
    public class SectorLabelTextFactory : ALabelTextFactoryBase {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.SectorIndex, "SectorIndex: {0}"},
                {LabelContentID.Density, "Density: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"}
            }}
            // TODO more LabelIDs
        };

        public SectorLabelTextFactory() : base() { }

        public bool TryMakeInstance(LabelID labelID, LabelContentID contentID, SectorReport report, SectorItemData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_String(report.Owner.LeaderName) : content;
                    break;
                case LabelContentID.SectorIndex:
                    content = report.SectorIndex.HasValue ? new ColoredTextList<Index3D>(report.SectorIndex.Value) : content;
                    break;
                case LabelContentID.Density:
                    content = report.Density.HasValue ? new ColoredTextList<float>(report.Density.Value) : content;
                    break;

                case LabelContentID.CameraDistance:
                    content = new ColoredTextList_Distance(data.Position);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyValue;
        }

        public SectorLabelText MakeInstance(LabelID labelID, SectorReport report, SectorItemData data) {
            var formatLookup = GetFormatLookup(labelID);
            SectorLabelText labelText = new SectorLabelText(labelID, report, _dedicatedLinePerContentIDLookup[labelID]);
            foreach (var contentID in formatLookup.Keys) {
                IColoredTextList content;
                if (TryMakeInstance(labelID, contentID, report, data, out content)) {
                    var format = formatLookup[contentID];
                    labelText.Add(contentID, content, format);
                }
            }
            return labelText;
        }

        protected override IDictionary<LabelContentID, string> GetFormatLookup(LabelID labelID) {
            return _formatLookupByLabelID[labelID];
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

