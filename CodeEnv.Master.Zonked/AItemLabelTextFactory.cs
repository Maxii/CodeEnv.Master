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
    [System.Obsolete]
    public abstract class AItemLabelTextFactory<ReportType, DataType> : ALabelTextFactory
        where ReportType : AItemReport
        where DataType : AItemData {

        public AItemLabelTextFactory() : base() { }

        public ALabelText MakeInstance(DisplayTargetID displayTgtID, ReportType report, DataType data) {
            var includedContentIDs = GetIncludedContentIDs(displayTgtID);
            ALabelText labelText = GenerateLabelText(displayTgtID, report, _dedicatedLinePerContentIDLookup[displayTgtID]);
            foreach (var contentID in includedContentIDs) {
                IColoredTextList content;
                if (TryMakeInstance(displayTgtID, contentID, report, data, out content)) {
                    string phrase;
                    if (!TryGetOverridePhrase(displayTgtID, contentID, out phrase)) {
                        phrase = GetDefaultPhrase(contentID);
                        //D.Log("{0} using default phrase [{1}] for {2}.", GetType().Name, phrase, contentID.GetName());
                    }
                    else {
                        D.Log("{0} using override phrase [{1}] for {2}.", GetType().Name, phrase, contentID.GetValueName());
                    }
                    labelText.Add(contentID, content, phrase);
                }
            }
            return labelText;
        }

        protected virtual ALabelText GenerateLabelText(DisplayTargetID displayTgtID, ReportType report, bool isDedicatedLinePerContentID) {
            return new LabelText(displayTgtID, report, isDedicatedLinePerContentID);
        }

        public abstract bool TryMakeInstance(DisplayTargetID displayTgtID, LabelContentID contentID, ReportType report, DataType data, out IColoredTextList content);

    }
}

