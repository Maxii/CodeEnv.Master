// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NoResearchTask.cs
// ResearchTask indicating no research underway. Replaces null.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// ResearchTask indicating no research underway. Replaces null.
    /// </summary>
    public class NoResearchTask : ResearchTask {

        public override string DebugName { get { return GetType().Name; } }

        public override float CompletionPercentage { get { return Constants.ZeroPercent; } }

        public override float CostToResearch { get { return Constants.ZeroF; } }

        public override AtlasID ImageAtlasID { get { return AtlasID.MyGui; } }

        public override string ImageFilename { get { return TempGameValues.EmptyImageFilename; } }

        public override GameTimeDuration TimeToComplete {
            get { throw new NotImplementedException("TimeToComplete.get is not implemented in {0}.".Inject(DebugName)); }
            set { throw new NotImplementedException("TimeToComplete.set is not implemented in {0}.".Inject(DebugName)); }
        }

        public NoResearchTask() : base(null) { }

        public override bool TryComplete(float scienceToApply, out float unconsumedScience) {
            throw new NotImplementedException("TryComplete() is not implemented in {0}.".Inject(DebugName));
        }


    }
}

