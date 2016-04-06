// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WaitForDate.cs
// YieldInstruction that waits until a current date is reached.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// YieldInstruction that waits until a current date is reached.
    /// Accommodates changes in GameSpeed during use.
    /// <remarks>More expensive than WaitForCalenderDate as GameTime.CurrentDate
    /// changes much more often than GameTime.CurrentCalenderDate.</remarks>
    /// </summary>
    public class WaitForDate : CustomYieldInstruction {

        private GameDate _targetDate;
        private GameTime _gameTime;

        public WaitForDate(GameDate date) {
            _gameTime = GameTime.Instance;
            _targetDate = date;
        }

        public override bool keepWaiting { get { return _gameTime.CurrentDate < _targetDate; } }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

