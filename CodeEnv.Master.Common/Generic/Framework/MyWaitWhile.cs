// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyWaitWhile.cs
// CustomYieldInstruction that waits until the predicate evaluates to false.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// CustomYieldInstruction that waits until the predicate evaluates to false.
    /// <remarks>Use WaitJobUtility.WaitWhileCondition() instead of making a new instance
    /// as WaitJobUtility handles IsRunning and IsPaused changes.</remarks>
    /// </summary>
    public class MyWaitWhile : APausableKillableYieldInstruction {

        private Func<bool> _predicate;

        public MyWaitWhile(Func<bool> predicate) {
            _predicate = predicate;
        }

        public override bool keepWaiting {
            get {
                if (_toKill) {
                    return false;
                }
                if (IsPaused) {
                    return true;
                }
                return _predicate();
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

