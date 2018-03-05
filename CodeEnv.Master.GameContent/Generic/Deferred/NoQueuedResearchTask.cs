// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NoQueuedResearchTask.cs
// ResearchTask for use with Players that have no research underway.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// ResearchTask for use with Players that have no research underway.
    /// </summary>
    [Obsolete]
    public class NoQueuedResearchTask : QueuedResearchTask {

        public override string Name { get { return GetType().Name; } }

        public override GameTimeDuration TimeToCompletion { get { return default(GameTimeDuration); } }

        public override GameDate ExpectedCompletionDate {
            get { throw new NotImplementedException("ExpectedCompletionDate.get is not implemented in {0}.".Inject(DebugName)); }
            set { throw new NotImplementedException("ExpectedCompletionDate.set is not implemented in {0}.".Inject(DebugName)); }
        }

        public override float CompletionPercentage { get { return Constants.ZeroPercent; } }

        public override AtlasID ImageAtlasID { get { return AtlasID.MyGui; } }

        public override string ImageFilename { get { return TempGameValues.EmptyImageFilename; } }

        public override float CostToResearch { get { return Constants.ZeroF; } }

        public NoQueuedResearchTask() : base(null) { }

        public override bool TryComplete(float scienceToApply, out float unconsumedScience) {
            throw new NotImplementedException("TryComplete() is not implemented in {0}.".Inject(DebugName));
        }
    }
}

