// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemReport.cs
//  Abstract base class for Item Reports.
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
    /// Abstract class for Item Reports.
    /// </summary>
    public abstract class AItemReport : AReport {

        public IntelCoverage IntelCoverage { get; private set; }

        public AItemReport(AItemData data, Player player)
            : base(player) {
            IntelCoverage = data.GetIntelCoverage(player);
            AssignValues(data);
        }

        private void AssignValues(AItemData data) {
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

        protected virtual void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageModerate(AItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageAware(AItemData data) { }
        protected virtual void AssignIncrementalValues_IntelCoverageNone(AItemData data) { }

    }
}

