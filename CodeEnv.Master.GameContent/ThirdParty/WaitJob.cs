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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interruptable Coroutine container customized for Waiting. Supports pausing but only 
    /// as a direct result of changes in the GameManager's Pause state. 
    /// </summary>
    public class WaitJob : Job {

        private IGameManager _gameMgr;

        public WaitJob(IEnumerator coroutine, bool toStart = false, Action<bool> onJobComplete = null)
            : base(coroutine, toStart, onJobComplete) {
            _gameMgr = References.GameManager;
        }

        /// <summary>
        /// Pauses this Job. Use only as a direct result of the game pausing.
        /// </summary>
        public override void Pause() {
            D.Assert(_gameMgr.IsPaused);
            base.Pause();
        }

        /// <summary>
        /// Unpauses this Job. Use only as a direct result of the game resuming.
        /// </summary>
        public override void Unpause() {
            D.Assert(!_gameMgr.IsPaused);
            base.Unpause();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

