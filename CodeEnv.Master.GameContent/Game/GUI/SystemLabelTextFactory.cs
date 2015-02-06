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
    public class SystemLabelTextFactory : ALabelTextFactoryBase {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.SectorIndex, "Sector: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Capacity, "Capacity: {0}"},
                {LabelContentID.Resources, "Resources: {0}"},
                {LabelContentID.Specials, "[800080]Specials:[-] {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"}
            }}
            // TODO more LabelIDs
        };

        public SystemLabelTextFactory() : base() { }

        public bool TryMakeInstance(LabelID labelID, LabelContentID contentID, SystemReport report, SystemData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.SectorIndex:
                    content = new ColoredTextList<Index3D>(report.SectorIndex);
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_String(report.Owner.LeaderName) : content;
                    break;
                case LabelContentID.Capacity:
                    content = report.Capacity.HasValue ? new ColoredTextList<int>(report.Capacity.Value) : content;
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
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
            return content != _emptyValue;
        }

        public SystemLabelText MakeInstance(LabelID labelID, SystemReport report, SystemData data) {
            var formatLookup = GetFormatLookup(labelID);
            SystemLabelText labelText = new SystemLabelText(labelID, report, _dedicatedLinePerContentIDLookup[labelID]);
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

