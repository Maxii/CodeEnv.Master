// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AApDestinationProxy.cs
/// Abstract base Proxy used by a Ship's AutoPilot to navigate to and/or attack targets.
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
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Destination.DebugName, GetType().Name);
                }
                return _debugName;
            }
        }

        public Vector3 Position { get; protected set; }

        public bool IsMobile { get { return Destination.IsMobile; } }

        public float ArrivalWindowDepth { get; private set; }

        /// <summary>
        /// Destination represented by this proxy is either a ship or a fleet.
        /// </summary>
        public bool IsFastMover { get; private set; }

        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }

        public IShipNavigable Destination { get; private set; }

        public bool HasArrived {
            get {
                float shipSqrdDistanceToDest = Vector3.SqrMagnitude(Position - _ship.Position);
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
        }

        /// <summary>
        /// The ship employing the proxy.
        /// </summary>
        protected IShip _ship;
        private IFleetFormationStation __formationStation;
        private float _innerRadiusSqrd;
        private float _outerRadiusSqrd;

        public AApDestinationProxy(IShipNavigable destination, IShip ship, float innerRadius, float outerRadius) {
            Utility.ValidateNotNull(destination);
            Utility.ValidateNotNegative(innerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity); // HACK
            Destination = destination;
            __formationStation = destination as IFleetFormationStation;
            IsFastMover = destination is IShip || destination is IFleetCmd;
            _ship = ship;
            InnerRadius = innerRadius;
            _innerRadiusSqrd = innerRadius * innerRadius;
            OuterRadius = outerRadius;
            _outerRadiusSqrd = outerRadius * outerRadius;
            ArrivalWindowDepth = outerRadius - innerRadius;
        }

        /// <summary>
        /// Returns <c>true</c> if direction and distance to arrival are valid, <c>false</c>
        /// if the ship has already arrived.
        /// </summary>
        /// <param name="shipPosition">The ship position.</param>
        /// <param name="direction">The direction to arrival.</param>
        /// <param name="distance">The distance to arrival.</param>
        /// <returns></returns>
        public bool TryGetArrivalDistanceAndDirection(out Vector3 direction, out float distance) {
            direction = Vector3.zero;
            distance = Constants.ZeroF;
            Vector3 vectorToDest = Position - _ship.Position;
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

    }
}

