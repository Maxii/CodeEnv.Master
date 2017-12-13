// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AApDestinationProxy.cs
// Abstract base Proxy used by a Ship's AutoPilot to navigate to and/or attack targets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base Proxy used by a Ship's AutoPilot to navigate to and/or attack targets.
    /// </summary>
    public abstract class AApDestinationProxy {

        private const string DebugNameFormat = "{0}.{1}";

        private string _debugName;
        public virtual string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Destination.DebugName, GetType().Name);
                }
                return _debugName;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if this ApProxy is a potentially uncatchable ship, <c>false</c> otherwise.
        /// <remarks>4.15.17 This proxy could also be uncatchable if its a fleet. However, ships that have fleets
        /// as a destination rely on their own FleetCmd to determine whether the fleet being pursued 
        /// is uncatchable. This makes for more coordinated decision making (the whole fleet decides to 
        /// abandon the pursuit rather than each ship making its own decision), and is more efficient.
        /// Thus the property name is not IsPotentiallyUncatchable.
        /// </remarks>
        /// </summary>
        public bool IsPotentiallyUncatchableShip { get; private set; }

        public Vector3 Position { get { return Destination.Position + _destOffset; } }

        public bool IsMobile { get { return Destination.IsMobile; } }

        public float ArrivalWindowDepth { get; private set; }

        /// <summary>
        /// Destination represented by this proxy is either a ship or a fleet.
        /// </summary>
        public bool IsFastMover { get; private set; }

        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }

        public IShipNavigableDestination Destination { get; private set; }

        public virtual float __ShipDistanceFromArrived {
            get {
                float shipDistanceToDest = Vector3.Distance(Position, _ship.Position);
                if (shipDistanceToDest > OuterRadius) {
                    return shipDistanceToDest - OuterRadius;
                }
                if (shipDistanceToDest < InnerRadius) {
                    return InnerRadius - shipDistanceToDest;
                }
                return Constants.ZeroF;
            }
        }

        public virtual bool HasArrived {
            get {
                float shipSqrdDistanceToDest = Vector3.SqrMagnitude(Position - _ship.Position);
                if (shipSqrdDistanceToDest.IsGreaterThan(_innerRadiusSqrd) && shipSqrdDistanceToDest.IsLessThan(_outerRadiusSqrd)) {
                    // ship has arrived
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The ship employing the proxy.
        /// </summary>
        protected IShip _ship;
        protected Vector3 _destOffset;

        private float _innerRadiusSqrd;
        private float _outerRadiusSqrd;

        public AApDestinationProxy(IShipNavigableDestination destination, IShip ship, Vector3 destOffset, float innerRadius, float outerRadius) {
            D.AssertNotNull(destination);
            Utility.ValidateNotNegative(innerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity); // HACK
            Destination = destination;
            IsFastMover = destination is IShip || destination is IFleetCmd;
            IsPotentiallyUncatchableShip = destination is IShip;
            _ship = ship;
            _destOffset = destOffset;
            InnerRadius = innerRadius;
            _innerRadiusSqrd = innerRadius * innerRadius;
            OuterRadius = outerRadius;
            _outerRadiusSqrd = outerRadius * outerRadius;
            ArrivalWindowDepth = outerRadius - innerRadius;
        }

        /// <summary>
        /// Returns <c>true</c> if still making progress toward the destination, <c>false</c>
        /// if progress is no longer being made as the ship has arrived. If still making
        /// progress, direction and distance to the destination is valid.
        /// </summary>
        /// <param name="direction">The direction to arrival.</param>
        /// <param name="distance">The distance to arrival.</param>
        /// <returns></returns>
        public virtual bool TryCheckProgress(out Vector3 direction, out float distance) {
            direction = Vector3.zero;
            distance = Constants.ZeroF;
            Vector3 vectorToDest = Position - _ship.Position;
            float sqrDistanceToDest = Vector3.SqrMagnitude(vectorToDest);
            if (sqrDistanceToDest.IsGreaterThan(_innerRadiusSqrd) && sqrDistanceToDest.IsLessThan(_outerRadiusSqrd)) {
                D.Assert(HasArrived, "{0} HasArrivedError: InnerSqrd {1}, SqrDistanceToDest {2}, OuterSqrd {3}.".Inject(DebugName, _innerRadiusSqrd, sqrDistanceToDest, _outerRadiusSqrd));
                // ship has arrived
                return false;
            }
            direction = vectorToDest.normalized;
            float distanceToDest = Mathf.Sqrt(sqrDistanceToDest);
            distance = distanceToDest - OuterRadius;
            if (distance.IsGreaterThan(Constants.ZeroF)) {
                // Effectively means distance > 0.0001 FloatEqualityPrecision
                return true;
            }
            if (distanceToDest.IsLessThan(InnerRadius)) {
                // Effectively means distanceToDest < InnerRadius - 0.0001 FloatEqualityPrecision
                direction = -direction;
                distance = InnerRadius - distanceToDest;
                return true;
            }
            // ship has arrived
            if (!HasArrived) {
                // 3.30.17 Received Inner = 0, Outer = 2, DistanceToDest = 2.000099. Warns because HasArrived uses squared values
                // 4.3.17 Received Inner = 12.8, Outer = 17, DistanceToDest = 12.79998. Warns because HasArrived uses squared values
                if (distanceToDest < InnerRadius - 0.001F || distanceToDest > OuterRadius + 0.001F) {    // 4.6.17 Makes meaningful warnings
                    D.Warn("{0} HasArrivedError: Inner {1}, DistanceToDest {2}, Outer {3}.", DebugName, InnerRadius, distanceToDest, OuterRadius);
                }
            }
            return false;
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

