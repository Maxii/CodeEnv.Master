// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemLabelTextFactory.cs
// Abstract generic class for LabelText Factories that support Items with PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract generic class for LabelText Factories that support Items with PlayerIntel.
    /// </summary>
    public abstract class AIntelItemLabelTextFactory<ReportType, DataType> : AItemLabelTextFactory<ReportType, DataType>
        where ReportType : AIntelItemReport
        where DataType : AIntelItemData {

        public AIntelItemLabelTextFactory() : base() { }

        protected override ALabelText GenerateLabelText(LabelID labelID, ReportType report, bool isDedicatedLinePerContentID) {
            return new IntelLabelText(labelID, report, isDedicatedLinePerContentID);
        }
    }
}

