// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemReport.cs
// Abstract class for Reports that support Items with PlayerIntel.
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
    ///  Abstract class for Reports that support Items with PlayerIntel.
    /// </summary>
    public abstract class AIntelItemReport : AItemReport {

        public AIntel Intel { get; private set; }

        public IntelCoverage IntelCoverage { get { return Intel.CurrentCoverage; } }

        public AIntelItemReport(AIntelItemData data, Player player, IIntelItem_Ltd item)
            : base(data, player, item) {
            Intel = data.GetIntelCopy(player);
            // IntelCoverage.None an occur as reports are rqstd when an element/cmd loses all IntelCoverage and the Cmd re-evaluates its icon
        }

        #region Archive

        //private void AssignValues(AItemData data) {
        //    switch (IntelCoverage) {
        //        case IntelCoverage.Comprehensive:
        //            AssignIncrementalValues_IntelCoverageComprehensive(data);
        //            goto case IntelCoverage.Broad;
        //        case IntelCoverage.Broad:
        //            AssignIncrementalValues_IntelCoverageBroad(data);
        //            goto case IntelCoverage.Essential;
        //        case IntelCoverage.Essential:
        //            AssignIncrementalValues_IntelCoverageEssential(data);
        //            goto case IntelCoverage.Basic;
        //        case IntelCoverage.Basic:
        //            AssignIncrementalValues_IntelCoverageBasic(data);
        //            goto case IntelCoverage.None;
        //        case IntelCoverage.None:
        //            AssignIncrementalValues_IntelCoverageNone(data);
        //            break;
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(IntelCoverage));
        //    }
        //}

        //protected virtual void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageBroad(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageEssential(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageBasic(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageNone(AItemData data) { }

        #endregion
    }
}

