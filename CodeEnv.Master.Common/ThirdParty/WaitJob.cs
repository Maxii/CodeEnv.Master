// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WaitJob.cs
// Interruptable Coroutine container customized for Waiting.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;

    /// <summary>
    /// Interruptable Coroutine container customized for Waiting. Does not support Pausing.
    /// </summary>
    public class WaitJob : Job {

        public WaitJob(IEnumerator coroutine, bool toStart = false, Action<bool> onJobComplete = null)
            : base(coroutine, toStart, onJobComplete) { }

        public override bool IsPaused { get { return false; } }

        public override void Pause() {
            throw new InvalidOperationException("{0} does not support Pausing.".Inject(GetType().Name));
        }

        public override void Unpause() {
            throw new InvalidOperationException("{0} does not support Pausing.".Inject(GetType().Name));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

