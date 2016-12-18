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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// CustomYieldInstruction that waits a specific number of hours. 
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>Use JobManager.WaitForHours() instead of making a new instance
    /// as JobManager handles IsRunning and IsPaused changes.</remarks>
    /// </summary>
    public class WaitForHours : APausableKillableYieldInstruction {

        private const string DebugNameFormat = "{0} TargetDate: {1}";

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(GetType().Name, _targetDate);
                }
                return _debugName;
            }
        }

        public float DurationInHours { get { return _duration.TotalInHours; } }

        protected GameDate _startDate;
        protected GameDate _targetDate;
        protected GameTime _gameTime;
        protected GameTimeDuration _duration;

        private Reference<GameTimeDuration> _durationRef;

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
                //D.Log("{0}.ReferenceValue is changing from {1} to {2}.", DebugName, _duration, _durationRef.Value);
                _duration = _durationRef.Value;
                _targetDate = new GameDate(_startDate, _duration);
            }
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        private bool __LogKeepWaiting() {
            var currentDate = _gameTime.CurrentDate;
            bool continueWaiting = currentDate < _targetDate;
            if (continueWaiting) {
                D.Log("{0}.keepWaiting called. CurrentDate {1} < TargetDate {2}, Frame {3}, Hours {4:0.00}.",
                    DebugName, currentDate, _targetDate, Time.frameCount, DurationInHours);
            }
            return continueWaiting;
        }

        #endregion
    }
}

