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
    public class StarLabelTextFactory : ALabelTextFactory<StarReport, StarData> {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.IntelCoverage, "IntelCoverage: {0}"},
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.ParentName, "ParentName: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},
                {LabelContentID.Category, "Category: {0}"},
                {LabelContentID.Capacity, "Capacity: {0}"},
                {LabelContentID.Resources, "Resources: {0}"},
                {LabelContentID.Specials, "[800080]Specials:[-] {0}"}, 

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public StarLabelTextFactory() : base() { }

        protected override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, StarReport report, StarData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.IntelCoverage:
                    content = new ColoredTextList_String(report.IntelCoverage.GetName());
                    break;
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
                    content = report.Category != StarCategory.None ? new ColoredTextList<StarCategory>(report.Category) : content;
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

