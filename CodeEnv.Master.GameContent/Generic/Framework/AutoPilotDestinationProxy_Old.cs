// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilotDestinationProxy.cs
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
    [System.Obsolete]
    public class AutoPilotDestinationProxy_Old {

        private const string DebugNameFormat = "{0}.{1}";

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Destination.DebugName, typeof(AutoPilotDestinationProxy_Old).Name);
                }
                return _debugName;
            }
        }

        public Vector3 Position { get { return Destination.Position + _destOffset; } }

        public bool IsMobile { get { return Destination.IsMobile; } }

        public float ArrivalWindowDepth { get; private set; }

        /// <summary>
        /// Destination represented by this proxy is either a ship or a fleet.
        /// </summary>
        public bool IsFastMover { get { return Destination is IShip || Destination is IFleetCmd; } }

        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }

        public IShipNavigable Destination { get; private set; }

        public Vector3 __DestinationOffset { get { return _destOffset; } }

        private IFleetFormationStation __formationStation;

        private Vector3 _destOffset;
        private float _innerRadiusSqrd;
        private float _outerRadiusSqrd;

        public AutoPilotDestinationProxy_Old(IShipNavigable destination, Vector3 destOffset, float innerRadius, float outerRadius) {
            Utility.ValidateNotNull(destination);
            Utility.ValidateNotNegative(innerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity); // HACK
            Destination = destination;
            __formationStation = destination as IFleetFormationStation;
            _destOffset = destOffset;
            InnerRadius = innerRadius;
            _innerRadiusSqrd = innerRadius * innerRadius;
            OuterRadius = outerRadius;
            _outerRadiusSqrd = outerRadius * outerRadius;
            ArrivalWindowDepth = outerRadius - innerRadius;
        }

        public bool HasArrived(Vector3 shipPosition) {
            float shipSqrdDistanceToDest = Vector3.SqrMagnitude(Position - shipPosition);
            if (shipSqrdDistanceToDest >= _innerRadiusSqrd && shipSqrdDistanceToDest <= _outerRadiusSqrd) {
                if (__formationStation != null) {
                    //D.Log("{0} is a {1} and is testing for IsOnStation.", DebugName, typeof(IFleetFormationStation).Name);
                    if (!__formationStation.IsOnStation) {
                        D.Warn(@"{0}: Inconsistent results between FormationStation.IsOnStation and its ApTgtProxy.HasArrived. 
                            /n_shipDistanceToDest = {1}, _innerRadius = {2}, _outerRadius = {3}, __DistanceFromStation = {4}.",
                            DebugName, Mathf.Sqrt(shipSqrdDistanceToDest), InnerRadius, OuterRadius, __formationStation.__DistanceFromOnStation);
                        return false;
                    }
                }
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
            // Note: Outer boundary of avoidableObstacleZone, if present, will usually be InnerRadius of Proxy
            // Warning: If InnerRadius is used here, the obstacleZone around the destination could be detected while 
            // within the 'HasArrived' window of the destination, thereby generating a detour just before "HasArrived".
            float rayLength = Vector3.Distance(Position, shipPosition) - OuterRadius;
            if (rayLength < Constants.ZeroF) {
                // 1.24.17 Physics.Raycast() using rayLength <= Zero results in false, aka no ray cast attempted
                return Constants.ZeroF;
            }
            return rayLength;
        }

        public void ResetOffset() {
            _destOffset = Vector3.zero;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

