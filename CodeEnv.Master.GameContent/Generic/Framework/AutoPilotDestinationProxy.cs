// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilotTarget.cs
// Proxy used by a Ship Helm's pilot to navigate to an IShipNavigable destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Proxy used by a Ship Helm's pilot to navigate to an IShipNavigable destination.
    /// </summary>
    public class AutoPilotDestinationProxy {

        private const string DebugNameFormat = "{0}.{1}";

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Destination.DebugName, typeof(AutoPilotDestinationProxy).Name);
                }
                return _debugName;
            }
        }

        public Vector3 Position { get { return Destination.Position + _destOffset; } }

        public bool IsMobile { get { return Destination.IsMobile; } }

        public float ArrivalWindowDepth { get; private set; }

        public bool IsFastMover { get { return Destination is IShip || Destination is IFleetCmd; } }

        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }

        public IShipNavigable Destination { get; private set; }

        private Vector3 _destOffset;
        private float _innerRadiusSqrd;
        private float _outerRadiusSqrd;

        public AutoPilotDestinationProxy(IShipNavigable destination, Vector3 destOffset, float innerRadius, float outerRadius) {
            Utility.ValidateNotNull(destination);
            Utility.ValidateNotNegative(innerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity); // HACK
            Destination = destination;
            _destOffset = destOffset;
            InnerRadius = innerRadius;
            _innerRadiusSqrd = innerRadius * innerRadius;
            OuterRadius = outerRadius;
            _outerRadiusSqrd = outerRadius * outerRadius;
            ArrivalWindowDepth = outerRadius - innerRadius;
        }

        public bool HasArrived(Vector3 shipPosition) {
            float shipSqrdDistanceToDest = Vector3.SqrMagnitude(Position - shipPosition);
            if (shipSqrdDistanceToDest.IsGreaterThanOrEqualTo(_innerRadiusSqrd) && shipSqrdDistanceToDest.IsLessThanOrEqualTo(_outerRadiusSqrd)) {
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
        /// <param name="direction">The direction to arrival.</param>
        /// <param name="distance">The distance to arrival.</param>
        /// <returns></returns>
        public bool TryGetArrivalDistanceAndDirection(Vector3 shipPosition, out Vector3 direction, out float distance) {
            direction = Vector3.zero;
            distance = Constants.ZeroF;
            Vector3 vectorToDest = Position - shipPosition;
            float distanceToDest = vectorToDest.magnitude;
            if (distanceToDest > InnerRadius && distanceToDest < OuterRadius) {
                // ship has arrived
                return false;
            }
            direction = vectorToDest.normalized;
            distance = distanceToDest - OuterRadius;
            if (distanceToDest < InnerRadius) {
                direction = -direction;
                distance = InnerRadius - distanceToDest;
            }
            return true;
        }

        public float GetObstacleCheckRayLength(Vector3 shipPosition) {
            return Vector3.Distance(Position, shipPosition) - OuterRadius;
        }

        public void ResetOffset() {
            _destOffset = Vector3.zero;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

