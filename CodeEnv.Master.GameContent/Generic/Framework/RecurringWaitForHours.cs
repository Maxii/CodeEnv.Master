// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RecurringWaitForHours.cs
// CustomYieldInstruction that waits a specific number of hours, allows the coroutine
// to continue, then waits again in perpetuity until killed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// CustomYieldInstruction that waits a specific number of hours, allows the coroutine
    /// to continue, then waits again in perpetuity until killed.
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>Use JobManager.RecurringWaitForHours() instead of making a new instance
    /// as JobManager handles IsRunning and IsPaused changes.</remarks>
    /// </summary>
    public class RecurringWaitForHours : WaitForHours {

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="durationRef">The duration reference.</param>
        public RecurringWaitForHours(Reference<GameTimeDuration> durationRef) : base(durationRef) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="hours">The hours.</param>
        public RecurringWaitForHours(float hours) : base(hours) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="duration">The duration.</param>
        public RecurringWaitForHours(GameTimeDuration duration) : base(duration) { }

        public override bool keepWaiting {
            get {
                bool continueWaiting = base.keepWaiting;
                if (!continueWaiting && !_toKill) {
                    RefreshValues();
                }
                return continueWaiting;
            }
        }

        private void RefreshValues() {
            _startDate = _gameTime.CurrentDate;
            var targetDate = new GameDate(_startDate, _duration);
            //D.Log("{0} is refreshing target date to {1}.", DebugName, targetDate);
            _targetDate = targetDate;
        }

    }
}

