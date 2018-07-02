// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FsmReturnHandler.cs
// Class that assists the FSM in handling Return()s from Call()ed states.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class that assists the FSM in handling Return()s from Call()ed states.
    /// </summary>
    public class FsmReturnHandler {

        private const string DebugNameFormat = "{0}.{1}";

        public string DebugName { get; private set; }

        /// <summary>
        /// The name of the Call()ed state this handler is configured for.
        /// <remark>Could also include the name of the Call()ing state.</remark>
        /// <remark>IMPROVE String rather than state to avoid having 4 versions of the class
        /// accommodating FleetState, BaseState, ShipState and FacilityState.</remark>
        /// </summary>
        public string CalledStateName { get; private set; }

        public FsmCallReturnCause ReturnCause { get; set; }

        private IDictionary<FsmCallReturnCause, Action> _returnCauseTaskLookup;

        public FsmReturnHandler(IDictionary<FsmCallReturnCause, Action> returnCauseTaskLookup, string calledStateName) {
            _returnCauseTaskLookup = returnCauseTaskLookup;
            CalledStateName = calledStateName;
            DebugName = DebugNameFormat.Inject(calledStateName, GetType().Name);
        }

        /// <summary>
        /// Processes the FsmCallReturnCause (aka the cause of the Return()) and executes the Task assigned to that
        /// Return() cause, if any. Returns <c>true</c> if the Return() has FsmCallReturnCause.None indicating the
        /// Call()ed state Return()ed upon successful completion and had no Task to execute, <c>false</c> if there was
        /// a FsmCallReturnCause besides None. An FsmCallReturnCause besides None indicates the Call()ed state
        /// Return()ed as a result of an event and did not successfully complete, resulting in execution of the Task
        /// associated with that FsmCallReturnCause.
        /// </summary>
        /// <param name="isWaitingToProcessReturn">if set to <c>true</c> [is waiting to process return].</param>
        /// <returns></returns>
        public bool WasCallSuccessful(ref bool isWaitingToProcessReturn) {
            FsmCallReturnCause unusedCause;
            return !TryProcessAndFindReturnCause(out unusedCause, ref isWaitingToProcessReturn);
        }

        /// <summary>
        /// Processes the FsmCallReturnCause (aka the cause of the Return()) and executes the Task assigned to that
        /// Return() cause, if any. Returns <c>false</c> if the Return() has FsmCallReturnCause.None indicating the
        /// Call()ed state Return()ed upon successful completion and no Task was processed, <c>true</c> if there was a
        /// FsmCallReturnCause besides None indicating the Call()ed state Return()ed as a result of an event and
        /// the Task associated with that Return() cause was processed.
        /// </summary>
        /// <param name="returnCause">The FsmCallReturnCause that was returned.</param>
        /// <param name="isWaitingToProcessReturn">if set to <c>true</c> [is waiting to process return].</param>
        /// <returns></returns>
        public bool TryProcessAndFindReturnCause(out FsmCallReturnCause returnCause, ref bool isWaitingToProcessReturn) {
            D.Assert(isWaitingToProcessReturn);
            isWaitingToProcessReturn = false;
            returnCause = ReturnCause;
            if (ReturnCause != default(FsmCallReturnCause)) {
                Action task;
                if (_returnCauseTaskLookup.TryGetValue(ReturnCause, out task)) {
                    task();
                }
                else {
                    D.Error("{0}: Unexpected ReturnCause {1}.", DebugName, ReturnCause.GetValueName());
                }
                return true;
            }
            return false;
        }


        public void Clear() {
            ReturnCause = default(FsmCallReturnCause);
            //D.Log("{0} has been cleared.", DebugName);
        }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        public void __Validate(string calledStateName) {
            D.AssertEqual(CalledStateName, calledStateName, "{0}: {1}.".Inject(DebugName, calledStateName));
            D.AssertDefault((int)ReturnCause);
        }

        #endregion

    }
}

