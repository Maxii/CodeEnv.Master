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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// A Ship orbit slot around orbitable bodies that knows how to place and remove a ship into/from orbit.
    /// </summary>
    public class ShipOrbitSlot : AOrbitSlot {

        private const float TravelDirectionAlignmentThreshold = 0.33F;

        public IShipOrbitable OrbitedObject { get; private set; }

        private IShipOrbitSimulator _orbitSimulator;
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
            : base(lowOrbitRadius, highOrbitRadius, orbitedObject.IsMobile, orbitPeriod) {
            OrbitedObject = orbitedObject;
            _orbitingShips = new List<IShipItem>();
        }

        /// <summary>
        /// Notifies the ShipOrbitSlot that the provided ship is preparing to assume orbit around <c>OrbitedObject</c>.
        /// Returns the OrbitSimulator for the ship to attach too.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <returns></returns>
        public IShipOrbitSimulator PrepareToAssumeOrbit(IShipItem ship) {
            D.Assert(_orbitSimulator != null);  // if null, then CheckPositionForOrbit wasn't used
            _orbitingShips.Add(ship);
            D.Log("{0} is preparing to assume orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            float shipOrbitRadius = Vector3.Distance(ship.Position, OrbitedObject.Position);
            if (!Contains(shipOrbitRadius)) {
                D.Warn("{0} is preparing to assume orbit around {2} but not within {3}. Ship's current orbit radius is {1}.", ship.FullName, shipOrbitRadius, OrbitedObject.FullName, this);
            }
            return _orbitSimulator;
        }

        /// <summary>
        /// Notifys the orbit slot that the provided ship has left orbit. If this is the last ship in orbit, the orbitSimulator is deactiveated/destroyed.
        /// </summary>
        /// <param name="ship">The ship that just left orbit.</param>
        public void HandleLeftOrbit(IShipItem ship) {
            D.Assert(_orbitSimulator != null);
            var isRemoved = _orbitingShips.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            float shipOrbitRadius = Vector3.Distance(ship.Position, OrbitedObject.Position);
            if (!Contains(shipOrbitRadius)) {
                D.Warn("{0} is leaving orbit of {1} but not from within its orbit slot. ShipOrbitRadius: {2}, OrbitSlot: {3}.",
                    ship.FullName, OrbitedObject.FullName, shipOrbitRadius, this);
            }
            if (_orbitingShips.Count == Constants.Zero) {
                D.Log("Destroying {0}'s {1}.", OrbitedObject.FullName, typeof(IOrbitSimulator).Name);
                UnityUtility.DestroyIfNotNullOrAlreadyDestroyed<IShipOrbitSimulator>(_orbitSimulator);
                _orbitSimulator = null;
                // IMPROVE could also keep it around for future uses as rigidbody.isKinematic = true so not using up physics engine cycles
            }
        }

        /// <summary>
        /// Tries to get approach direction and speed. Returns <c>true</c> if an approach was calculated and returned,
        /// <c>false</c> if no approach is possible as the ship is already within the orbit slot.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="approachDirection">The approach direction.</param>
        /// <param name="approachSpeed">The approach speed.</param>
        /// <returns></returns>
        public bool TryGetApproach(IShipItem ship, out Vector3 approachDirection, out Speed approachSpeed) {
            if (_orbitSimulator == null) {
                D.Assert(_orbitingShips.Count == Constants.Zero);
                GameObject orbitedObjectGo = OrbitedObject.transform.gameObject;
                _orbitSimulator = References.GeneralFactory.MakeOrbitSimulatorInstance(orbitedObjectGo, IsOrbitedObjectMobile, true, _orbitPeriod, "ShipOrbitSimulator") as IShipOrbitSimulator;
                _orbitSimulator.IsActivelyOrbiting = true;
            }

            float shipDistanceToOrbitedObject;
            if(CheckPositionForOrbit(ship, out shipDistanceToOrbitedObject)) {
                approachDirection = Vector3.zero;
                approachSpeed = Speed.None;
                return false;
            }

            Vector3 directionToOrbitedObject = (OrbitedObject.Position - ship.Position).normalized;
            float distanceToOrbitSlotMean = shipDistanceToOrbitedObject - MeanRadius;
            approachDirection = distanceToOrbitSlotMean > Constants.ZeroF ? directionToOrbitedObject : -directionToOrbitedObject;
            approachSpeed = Speed.StationaryOrbit;
            var movingShipOrbitSimulator = _orbitSimulator as IMovingShipOrbitSimulator;
            if (movingShipOrbitSimulator != null) {
                Vector3 orbitSlotTravelDirection = movingShipOrbitSimulator.DirectionOfTravel;
                var directionAlignment = Vector3.Dot(approachDirection, orbitSlotTravelDirection);   // 1 = same, -1 = opposite, 0 = orthoganol
                if (directionAlignment < Constants.ZeroF) {
                    // orbitSlot is moving towards the ship
                    approachSpeed = Speed.StationaryOrbit;
                }
                else if (directionAlignment < TravelDirectionAlignmentThreshold) {
                    // orbitSlot moving orthogonal to or partially away from the ship
                    approachSpeed = Speed.MovingOrbit;
                }
                else {
                    // orbitSlot moving mostly away from the ship
                    approachSpeed = Speed.Slow;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks the ship's position to see if it is within the OrbitSlot. Returns <c>true</c> if the ship
        /// has arrived within the orbit slot, <c>false</c> otherwise.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <returns></returns>
        public bool CheckPositionForOrbit(IShipItem ship) {
            float unused;
            return CheckPositionForOrbit(ship, out unused);
        }

        /// <summary>
        /// Checks the ship's position to see if it is within the OrbitSlot. Returns <c>true</c> if the ship
        /// has arrived within the orbit slot, <c>false</c> otherwise.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipDistanceToOrbitedObject">The ship distance to orbited object. Always valid.</param>
        /// <returns></returns>
        private bool CheckPositionForOrbit(IShipItem ship, out float shipDistanceToOrbitedObject) {
            shipDistanceToOrbitedObject = Vector3.Distance(ship.Position, OrbitedObject.Position);
            return Contains(shipDistanceToOrbitedObject);
        }

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

