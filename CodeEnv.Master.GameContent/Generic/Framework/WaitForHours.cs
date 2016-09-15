// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WaitForHours.cs
// YieldInstruction that waits a specific number of hours.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// CustomYieldInstruction that waits a specific number of hours. 
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>Use WaitJobUtility.WaitForHours() instead of making a new instance
    /// as WaitJobUtility handles IsRunning and IsPaused changes.</remarks>
    /// </summary>
    public class WaitForHours : APausableKillableYieldInstruction {

        private const string ToStringFormat = "{0} TargetDate: {1}";

        private Reference<GameTimeDuration> _durationRef;
        private GameDate _startDate;

        private GameDate _targetDate;
        private GameTime _gameTime;
        private GameTimeDuration _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="durationRef">The duration reference.</param>
        public WaitForHours(Reference<GameTimeDuration> durationRef) : this(durationRef.Value) {
            _durationRef = durationRef;
            _startDate = _gameTime.CurrentDate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="hours">The hours.</param>
        public WaitForHours(float hours) : this(new GameTimeDuration(hours)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForHours"/> class.
        /// </summary>
        /// <param name="duration">The duration.</param>
        public WaitForHours(GameTimeDuration duration) {
            _gameTime = GameTime.Instance;
            _targetDate = new GameDate(duration);
            _duration = duration;
        }

        public override bool keepWaiting {
            get {
                if (_toKill) {
                    return false;
                }
                if (IsPaused) {
                    return true;
                }
                HandleAnyDurationChange();
                //return __LogKeepWaiting();
                return _gameTime.CurrentDate < _targetDate;
            }
        }

        private void HandleAnyDurationChange() {
            if (_durationRef == null) {
                return;
            }
            if (_durationRef.Value != _duration) {
                // duration has changed
                _duration = _durationRef.Value;
                _targetDate = new GameDate(_startDate, _duration);
            }
        }

        public override string ToString() {
            return ToStringFormat.Inject(typeof(WaitForHours), _targetDate);
        }

        #region Debug

        private bool __LogKeepWaiting() {
            var currentDate = _gameTime.CurrentDate;
            bool continueWaiting = currentDate < _targetDate;
            D.Log(continueWaiting, "WaitForHours.keepWaiting called. CurrentDate {0} < TargetDate {1}, Frame {2}, Hours {3:0.00}.",
                currentDate, _targetDate, Time.frameCount, _duration.TotalInHours);
            return continueWaiting;
        }

        #endregion
    }
}

