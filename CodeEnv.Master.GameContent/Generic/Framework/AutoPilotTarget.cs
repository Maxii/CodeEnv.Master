// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilotTarget.cs
// Target used by the ShipHelm's auto pilot to navigate to an INavigable destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Target used by the ShipHelm's auto pilot to navigate to an INavigable destination.
    /// </summary>
    public class AutoPilotTarget {

        private const string NameFormat = "{0}.{1}";

        public string FullName { get { return NameFormat.Inject(Target.FullName, typeof(AutoPilotTarget).Name); } }

        public Vector3 Position { get { return Target.Position + _targetOffset; } }

        public bool IsMobile { get { return Target.IsMobile; } }

        public float ArrivalWindowDepth { get; private set; }

        public bool IsFastMover { get { return Target is IShipItem || Target is IFleetCmdItem; } }

        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }

        public IShipNavigable Target { get; private set; }

        private Vector3 _targetOffset;

        public AutoPilotTarget(IShipNavigable target, Vector3 targetOffset, float innerRadius, float outerRadius) {
            Target = target;
            _targetOffset = targetOffset;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            ArrivalWindowDepth = outerRadius - innerRadius;
        }

        public bool IsArrived(Vector3 shipPosition) {
            float distanceToTarget = Vector3.Distance(Position, shipPosition);
            if (distanceToTarget > InnerRadius && distanceToTarget < OuterRadius) {
                // ship has arrived
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if direction and distance to arrival are valid, <c>false</c>
        /// if the ship has already arrived.
        /// </summary>
        /// <param name="shipPosition">The ship position.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        public bool TryGetArrivalDistanceAndDirection(Vector3 shipPosition, out Vector3 direction, out float distance) {
            direction = Vector3.zero;
            distance = Constants.ZeroF;
            Vector3 vectorToTarget = Position - shipPosition;
            float distanceToTarget = vectorToTarget.magnitude;
            if (distanceToTarget > InnerRadius && distanceToTarget < OuterRadius) {
                // ship has arrived
                return false;
            }
            direction = vectorToTarget.normalized;
            distance = distanceToTarget - OuterRadius;
            if (distanceToTarget < InnerRadius) {
                direction = -direction;
                distance = InnerRadius - distanceToTarget;
            }
            return true;
        }

        public float GetObstacleCheckRayLength(Vector3 shipPosition) {
            return Vector3.Distance(Position, shipPosition) - OuterRadius;
        }

        public void ResetOffset() {
            _targetOffset = Vector3.zero;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

