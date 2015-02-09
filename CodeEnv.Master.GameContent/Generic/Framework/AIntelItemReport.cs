// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemReport.cs
//  Abstract class for Reports associated with an AIntelItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Abstract class for Reports associated with an AIntelItem.
    /// </summary>
    public abstract class AIntelItemReport : AItemReport {

        public IntelCoverage IntelCoverage { get; private set; }

        public AIntelItemReport(AIntelItemData data, Player player)
            : base(player) {
            IntelCoverage = data.GetIntelCoverage(player);
            AssignValues(data);
        }

        private void AssignValues(AIntelItemData data) {
            switch (IntelCoverage) {
                case IntelCoverage.Comprehensive:
                    AssignIncrementalValues_IntelCoverageComprehensive(data);
                    goto case IntelCoverage.Moderate;
                case IntelCoverage.Moderate:
                    AssignIncrementalValues_IntelCoverageModerate(data);
                    goto case IntelCoverage.Minimal;
                case IntelCoverage.Minimal:
                    AssignIncrementalValues_IntelCoverageMinimal(data);
                    goto case IntelCoverage.Aware;
                case IntelCoverage.Aware:
                    AssignIncrementalValues_IntelCoverageAware(data);
                    goto case IntelCoverage.None;
                case IntelCoverage.None:
                    AssignIncrementalValues_IntelCoverageNone(data);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(IntelCoverage));
            }
        }

        protected virtual void AssignIncrementalValues_IntelCoverageComprehensive(AIntelItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageModerate(AIntelItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageMinimal(AIntelItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageAware(AIntelItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageNone(AIntelItemData data) { }

    }
}

