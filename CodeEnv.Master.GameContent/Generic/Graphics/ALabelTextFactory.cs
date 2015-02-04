// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelTextGenerator.cs
// Abstract generic class for LabelText Factories.
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
    /// Abstract generic class for LabelText Factories.
    /// </summary>
    public abstract class ALabelTextFactory<ReportType, DataType> : ALabelTextFactoryBase
        where ReportType : AItemReport
        where DataType : AItemData {

        public ALabelTextFactory() : base() { }

        public LabelText MakeInstance(LabelID labelID, ReportType report, DataType data) {
            var formatLookup = GetFormatLookup(labelID);
            LabelText labelText = new LabelText(labelID, report, _dedicatedLinePerContentIDLookup[labelID]);
            foreach (var contentID in formatLookup.Keys) {
                IColoredTextList content;
                if (TryMakeInstance(labelID, contentID, report, data, out content)) {
                    var format = formatLookup[contentID];
                    labelText.Add(contentID, content, format);
                }
            }
            return labelText;
        }

        protected abstract bool TryMakeInstance(LabelID labelID, LabelContentID contentID, ReportType report, DataType data, out IColoredTextList content);

        public IColoredTextList MakeInstance(LabelID labelID, LabelContentID contentID, DataType data) {
            ValidateIDs(labelID, contentID);
            switch (contentID) {
                case LabelContentID.CameraDistance:
                    return new ColoredTextList<float>(data.Position.DistanceToCamera());
                case LabelContentID.IntelState:
                    return new ColoredTextList_Intel(data.HumanPlayerIntel);
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
            }
        }

    }
}

