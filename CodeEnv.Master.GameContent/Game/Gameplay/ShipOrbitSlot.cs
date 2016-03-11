// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrbitSlot.cs
// A Ship orbit slot around orbitable bodies that knows how to place and remove a ship into/from orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// A Ship orbit slot around orbitable bodies that knows how to place and remove a ship into/from orbit.
    /// </summary>
    public class ShipOrbitSlot : AOrbitSlot {

        private const float TravelDirectionAlignmentThreshold = 0.33F;

        public IShipOrbitable OrbitedObject { get; private set; }

        public IShipOrbitSimulator OrbitSimulator { get; private set; }

        private IList<IShipItem> _orbitingShips;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot"/> class. 
        /// The OrbitPeriod defaults to OneYear.
        /// </summary>
        /// <param name="lowOrbitRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="highOrbitRadius">The radius at this slot's highest orbit.</param>
        /// <param name="orbitedObject">The orbited object.</param>
        public ShipOrbitSlot(float lowOrbitRadius, float highOrbitRadius, IShipOrbitable orbitedObject)
            : this(lowOrbitRadius, highOrbitRadius, orbitedObject, GameTimeDuration.OneYear) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot" /> class.
        /// </summary>
        /// <param name="lowOrbitRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="highOrbitRadius">The radius at this slot's highest orbit.</param>
        /// <param name="orbitedObject">The object being orbited.</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public ShipOrbitSlot(float lowOrbitRadius, float highOrbitRadius, IShipOrbitable orbitedObject, GameTimeDuration orbitPeriod)
            : base(lowOrbitRadius, highOrbitRadius, orbitedObject.IsMobile, orbitPeriod, toOrbit: true) {
            OrbitedObject = orbitedObject;
            _orbitingShips = new List<IShipItem>();
        }

        public void AssumeOrbit(IShipItem ship, FixedJoint shipJoint) {
            D.Assert(OrbitSimulator != null);  // if null, then TryDetermineOrbitAchievableViaAutoPilot wasn't used
            _orbitingShips.Add(ship);
            float shipDistance = Vector3.Distance(ship.Position, OrbitedObject.Position);
            float minOutsideOfOrbitCaptureRadius = OuterRadius - ship.CollisionDetectionZoneRadius;
            D.Warn(shipDistance.IsGreaterThanOrEqualTo(minOutsideOfOrbitCaptureRadius), "{0} is assuming orbit around {1} but is not within {2:0.0000}. Ship's current orbit distance is {3:0.0000}.",
                ship.FullName, OrbitedObject.FullName, minOutsideOfOrbitCaptureRadius, shipDistance);
            shipJoint.connectedBody = OrbitSimulator.Rigidbody;
            //D.Log("{0} has assumed orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            if (_orbitingShips.Count == Constants.One) {
                OrbitSimulator.IsActivated = true;
            }
        }

        /// <summary>
        /// Flag indicating whether the provided ship is in orbit.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <returns></returns>
        public bool IsInOrbit(IShipItem ship) {
            return _orbitingShips.Contains(ship);
        }

        /// <summary>
        /// Notifys the orbit slot that the provided ship has left orbit. 
        /// If this is the last ship in orbit, the orbitSimulator is deactiveated or destroyed.
        /// </summary>
        /// <param name="ship">The ship that just left orbit.</param>
        public void HandleLeftOrbit(IShipItem ship) {
            D.Assert(OrbitSimulator != null);
            var isRemoved = _orbitingShips.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            float shipDistance = Vector3.Distance(ship.Position, OrbitedObject.Position);
            float minOutsideOfOrbitCaptureRadius = OuterRadius - ship.CollisionDetectionZoneRadius;
            D.Warn(shipDistance.IsGreaterThanOrEqualTo(minOutsideOfOrbitCaptureRadius), "{0} is leaving orbit of {1} but is not within {2:0.0000}. Ship's current orbit distance is {3:0.0000}.",
                ship.FullName, OrbitedObject.FullName, minOutsideOfOrbitCaptureRadius, shipDistance);

            if (_orbitingShips.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                OrbitSimulator.IsActivated = false;
                //DestroySimulator();
            }
        }

        private void DestroySimulator() {
            D.Log("Destroying {0}'s {1}.", OrbitedObject.FullName, typeof(IShipOrbitSimulator).Name);
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed<IShipOrbitSimulator>(OrbitSimulator);
            OrbitSimulator = null;
        }

        /// <summary>
        /// Tries to determine whether an orbit can be achieved using the AutoPilot. Returns <c>true</c> if 
        /// the ship is outside the capture radius of the orbit slot and the AutoPilot can be used, 
        /// <c>false</c> if the ship is already inside the orbitslot capture radius and therefore the AutoPilot
        /// cannot be used.
        /// <remarks>If the ship is already properly positioned within the orbit slot capture window or it is inside
        /// the capture window (probably encountering an IObstacle zone except around bases), the AutoPilot cannot be used.
        /// In this case it is probably best to simply place the ship where it belongs as this should not happen often.</remarks>
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="approachSpeed">The approach speed.</param>
        /// <returns></returns>
        public bool TryDetermineOrbitAchievableViaAutoPilot(IShipItem ship, out Speed approachSpeed) {
            if (OrbitSimulator == null) {
                D.Assert(_orbitingShips.Count == Constants.Zero);
                OrbitSimulator = References.GeneralFactory.MakeShipOrbitSimulatorInstance(this);
            }

            float shipDistanceToOrbitedObject;
            if (CheckPositionForOrbitViaAutoPilot(ship, out shipDistanceToOrbitedObject)) {
                approachSpeed = Speed.StationaryOrbit;
                IMobileShipOrbitSimulator mobileShipOrbitSim = OrbitSimulator as IMobileShipOrbitSimulator;
                if (mobileShipOrbitSim != null) {
                    D.Assert(IsOrbitedObjectMobile);
                    Vector3 orbitedObjectTravelDirection = mobileShipOrbitSim.DirectionOfTravel;
                    Vector3 shipTravelDirection = (OrbitedObject.Position - ship.Position).normalized;
                    float directionAlignment = Vector3.Dot(shipTravelDirection, orbitedObjectTravelDirection);   // 1 = same, -1 = opposite, 0 = orthoganol
                    if (directionAlignment < Constants.ZeroF) {
                        // orbitedObject is moving towards the ship
                        approachSpeed = Speed.StationaryOrbit;
                    }
                    else if (directionAlignment < TravelDirectionAlignmentThreshold) {
                        // orbitedObject is moving orthogonal to or partially away from the ship
                        approachSpeed = Speed.MovingOrbit;
                    }
                    else {
                        // orbitObject is moving mostly away from the ship
                        approachSpeed = Speed.Slow;
                    }
                }
                return true;
            }
            // Ship is inside orbit slot capture window so AutoPilot can't be used
            approachSpeed = Speed.None;
            return false;
        }

        /// <summary>
        /// Checks the ship's position to see if the autopilot can be used to achieve orbit. Returns <c>true</c> if the ship
        /// if located outside the orbitslot capture window, <c>false</c> otherwise.
        /// <remarks>If the ship is already properly positioned within the orbit slot capture window or it is inside
        /// the capture window (probably encountering an IObstacle zone except around bases), the AutoPilot cannot be used.
        /// In this case it is probably best to simply place the ship where it belongs as this should not happen often.</remarks>
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipDistanceToOrbitedObject">The ship distance to orbited object. Always valid.</param>
        /// <returns></returns>
        private bool CheckPositionForOrbitViaAutoPilot(IShipItem ship, out float shipDistanceToOrbitedObject) {
            shipDistanceToOrbitedObject = Vector3.Distance(ship.Position, OrbitedObject.Position);
            float minOutsideOfOrbitCaptureRadius = OuterRadius - ship.CollisionDetectionZoneRadius;
            return shipDistanceToOrbitedObject > minOutsideOfOrbitCaptureRadius;
        }

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

