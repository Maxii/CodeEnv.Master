// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemLabelTextFactory.cs
// Abstract generic class for LabelText Factories that support Items with no PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for LabelText Factories that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemLabelTextFactory<ReportType, DataType> : ALabelTextFactory
        where ReportType : AItemReport
        where DataType : AItemData {

        public AItemLabelTextFactory() : base() { }

        public ALabelText MakeInstance(LabelID labelID, ReportType report, DataType data) {
            var includedContentIDs = GetIncludedContentIDs(labelID);
            ALabelText labelText = GenerateLabelText(labelID, report, _dedicatedLinePerContentIDLookup[labelID]);
            foreach (var contentID in includedContentIDs) {
                IColoredTextList content;
                if (TryMakeInstance(labelID, contentID, report, data, out content)) {
                    string phrase;
                    if (!TryGetOverridePhrase(labelID, contentID, out phrase)) {
                        phrase = GetDefaultPhrase(contentID);
                        //D.Log("{0} using default phrase [{1}] for {2}.", GetType().Name, phrase, contentID.GetName());
                    }
                    else {
                        D.Log("{0} using override phrase [{1}] for {2}.", GetType().Name, phrase, contentID.GetName());
                    }
                    labelText.Add(contentID, content, phrase);
                }
            }
            return labelText;
        }

        protected virtual ALabelText GenerateLabelText(LabelID labelID, ReportType report, bool isDedicatedLinePerContentID) {
            return new LabelText(labelID, report, isDedicatedLinePerContentID);
        }

        public abstract bool TryMakeInstance(LabelID labelID, LabelContentID contentID, ReportType report, DataType data, out IColoredTextList content);

    }
}

