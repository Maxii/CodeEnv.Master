﻿// --------------------------------------------------------------------------------------------------------------------
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

        public override Technology Tech {
            get { throw new NotImplementedException("Tech.get is not implemented in {0}.".Inject(DebugName)); }
        }

        public override float RemainingScienceNeededToComplete {
            get {
                D.Warn("{0}: Accessing RemainingScienceNeededToComplete.", DebugName);
                return base.RemainingScienceNeededToComplete;
            }
        }

        public override float CumScienceApplied {
            get {
                D.Warn("{0}: Accessing CumScienceApplied.", DebugName);
                return base.CumScienceApplied;
            }
        }

        public NoResearchTask() : base(null) { }

        public override bool TryComplete(float scienceToApply, out float unconsumedScience) {
            throw new NotImplementedException("TryComplete() is not implemented in {0}.".Inject(DebugName));
        }

        public override void CompleteResearch() {
            throw new NotImplementedException("CompleteResearch() is not implemented in {0}.".Inject(DebugName));
        }
    }
}

