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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// YieldInstruction that waits a specific number of hours. 
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>Use Yielders.GetWaitForHours() instead of making a new instance
    /// as each new instance creates garbage.</remarks>
    /// </summary>
    public class WaitForHours : CustomYieldInstruction {

        private const string ToStringFormat = "{0} TargetDate: {1}";

        private GameDate _targetDate;
        private GameTime _gameTime;

        public WaitForHours(float hours) : this(new GameTimeDuration(hours)) { }

        public WaitForHours(GameTimeDuration duration) {
            _gameTime = GameTime.Instance;
            _targetDate = new GameDate(duration);
        }

        public override bool keepWaiting {
            get {
                //return LogKeepWaiting();
                return _gameTime.CurrentDate < _targetDate;
            }
        }

        private bool LogKeepWaiting() {
            var currentDate = _gameTime.CurrentDate;
            bool continueWaiting = currentDate < _targetDate;
            D.Log(continueWaiting, "WaitForHours.keepWaiting called. CurrentDate {0} < TargetDate {1}, Frame {2}.",
                currentDate, _targetDate, Time.frameCount);
            return continueWaiting;
        }

        public override string ToString() {
            return ToStringFormat.Inject(typeof(WaitForHours), _targetDate);
        }

    }
}

