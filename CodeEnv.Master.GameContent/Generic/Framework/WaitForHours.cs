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
    /// YieldInstruction that waits a specific number of hours. 
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>Expensive as GameTime.CurrentDate
    /// changes much more often than GameTime.CurrentCalenderDate.</remarks>
    /// </summary>
    public class WaitForHours : CustomYieldInstruction {

        private GameDate _targetDate;
        private GameTime _gameTime;

        public WaitForHours(float hours) {
            _gameTime = GameTime.Instance;
            _targetDate = new GameDate(new GameTimeDuration(hours));
        }

        public override bool keepWaiting { get { return _gameTime.CurrentDate < _targetDate; } }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

