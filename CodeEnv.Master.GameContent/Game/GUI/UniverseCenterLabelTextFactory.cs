// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterLabelTextFactory.cs
// LabelText factory for the UniverseCenter.
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
    /// LabelText factory for the UniverseCenter.
    /// </summary>
    public class UniverseCenterLabelTextFactory : ALabelTextFactory<UniverseCenterReport, UniverseCenterData> {

        private static IDictionary<LabelID, IDictionary<LabelContentID, string>> _formatLookupByLabelID = new Dictionary<LabelID, IDictionary<LabelContentID, string>>() {
            { LabelID.CursorHud, new Dictionary<LabelContentID, string>() {
                {LabelContentID.IntelCoverage, "IntelCoverage: {0}"},
                {LabelContentID.Name, "Name: {0}"},
                {LabelContentID.Owner, "Owner: {0}"},

                {LabelContentID.CameraDistance, "CameraDistance: {0}"},
                {LabelContentID.IntelState, "< {0} >"}
            }}
            // TODO more LabelIDs
        };

        public UniverseCenterLabelTextFactory() : base() { }

        protected override bool TryMakeInstance(LabelID labelID, LabelContentID contentID, UniverseCenterReport report, UniverseCenterData data, out IColoredTextList content) {
            content = _includeUnknownLookup[labelID] ? _unknownValue : _emptyValue;
            switch (contentID) {
                case LabelContentID.IntelCoverage:
                    content = new ColoredTextList_String(report.IntelCoverage.GetName());
                    break;
                case LabelContentID.Name:
                    content = !report.Name.IsNullOrEmpty() ? new ColoredTextList_String(report.Name) : content;
                    break;
                case LabelContentID.Owner:
                    content = report.Owner != null ? new ColoredTextList_String(report.Owner.LeaderName) : content;
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

