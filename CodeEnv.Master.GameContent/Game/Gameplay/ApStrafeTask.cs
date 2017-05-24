// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApStrafeTask.cs
// AutoPilot task that moves to and tracks a target while strafing it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// AutoPilot task that moves to and tracks a target while strafing it.
    /// </summary>
    public class ApStrafeTask : ApMoveTask {

        protected new ApStrafeDestinationProxy TargetProxy { get { return base.TargetProxy as ApStrafeDestinationProxy; } }

        protected override bool ToEliminateDrift { get { return false; } }

        protected internal override bool IsFleetwideMove {
            protected get { return false; }
            set { throw new NotSupportedException(); }
        }

        protected override float SafeArrivalWindowCaptureDepth { get { return TargetProxy.ArrivalWindowDepth * 0.75F; } }

        public ApStrafeTask(AutoPilot autoPilot) : base(autoPilot) { }

        public override void Execute(ApMoveDestinationProxy attackTgtProxy, Speed speed) {
            base.Execute(attackTgtProxy, speed);
        }

        protected override void ResumeDirectCourseToTarget() {
            TargetProxy.RefreshStrafePosition();
            base.ResumeDirectCourseToTarget();
        }

        protected override void HandleTargetReached() {
            //D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, TargetFullName, TargetProxy.Position, TargetDistance);
            D.Log(ShowDebugLog, "{0} is strafing {1}.", DebugName, TargetFullName);
            _autoPilot.RefreshCourse(CourseRefreshMode.NewCourse);
            var beginRunWaypoint = TargetProxy.GenerateBeginRunWaypoint();
            _autoPilot.RefreshCourse(CourseRefreshMode.AddWaypoint, beginRunWaypoint);
            ContinueCourseToTargetVia(beginRunWaypoint);
        }

        public override void ResetForReuse() {
            base.ResetForReuse();
        }

        protected override void Cleanup() {
            base.Cleanup();
        }

    }
}

