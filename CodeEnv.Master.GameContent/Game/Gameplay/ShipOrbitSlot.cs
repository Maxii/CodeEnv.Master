// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrbitSlot.cs
// Class for Ship orbit slots around other bodies that knows how to place
// and remove a ship into/from orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Ship orbit slots around other bodies that knows how to place
    /// and remove a ship into/from orbit.
    /// </summary>
    public class ShipOrbitSlot : AOrbitSlot {

        /// <summary>
        /// Raised if and when <c>OrbitedObject</c> dies. Ships should assign their
        /// method that breaks them from orbit to this event. The event automatically 
        /// unsubscribes all subscribers after it is called.
        /// </summary>
        public event Action onOrbitedObjectDeathOneShot;

        public IShipOrbitable OrbitedObject { get; private set; }

        private IOrbiterForShips _orbiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot"/> class. 
        /// The OrbitPeriod defaults to OneYear.
        /// </summary>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="outerRadius">The outer radius.</param>
        /// <param name="orbitedObject">The orbited object.</param>
        public ShipOrbitSlot(float innerRadius, float outerRadius, IShipOrbitable orbitedObject)
            : this(innerRadius, outerRadius, orbitedObject, GameTimeDuration.OneYear) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot" /> class.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="orbitedObject">The object being orbited.</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public ShipOrbitSlot(float innerRadius, float outerRadius, IShipOrbitable orbitedObject, GameTimeDuration orbitPeriod)
            : base(innerRadius, outerRadius, orbitedObject.IsMobile, orbitPeriod) {
            OrbitedObject = orbitedObject;
            var mortalOrbitedObject = orbitedObject as IMortalItem;
            if (mortalOrbitedObject != null) {
                mortalOrbitedObject.onDeathOneShot += OnOrbitedObjectDeath;
            }
        }

        /// <summary>
        /// Places the ship within this orbit and commences orbital movement if not already underway.
        /// </summary>
        /// <param name="ship">The ship.</param>
        public void AssumeOrbit(IShipItem ship) {
            if (_orbiter == null) {
                GameObject orbitedObjectGo = OrbitedObject.Transform.gameObject;
                _orbiter = References.GeneralFactory.MakeOrbiterInstance(orbitedObjectGo, _isOrbitedObjectMobile, true, _orbitPeriod, "ShipOrbiter") as IOrbiterForShips;
            }
            AttachShipToOrbit(ship);
            _orbiter.enabled = true;
            D.Log("{0} has assumed orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            float shipOrbitRadius = Vector3.Distance(ship.Position, OrbitedObject.Position);
            if (!Contains(shipOrbitRadius)) {
                D.Warn("{0} has assumed orbit around {2} but not within {3}. Ship's current orbit radius is {1}.", ship.FullName, shipOrbitRadius, OrbitedObject.FullName, this);
            }
        }

        private void AttachShipToOrbit(IShipItem ship) {
            ship.Transform.parent = _orbiter.Transform; // ship retains existing position, rotation, scale and layer
        }

        /// <summary>
        /// Breaks the ship from this orbit. If this was the last ship in orbit, the orbital movement is disabled
        /// until a ship again assumes orbit.
        /// </summary>
        /// <param name="orbitingShip">The orbiting ship.</param>
        public void BreakOrbit(IShipItem orbitingShip) {
            D.Assert(_orbiter != null);
            //D.Log("{0} attempting to break orbit around {1}.", orbitingShip.FullName, OrbitedObject.FullName);
            var orbitingShips = _orbiter.Transform.gameObject.GetSafeInterfacesInChildren<IShipItem>();
            var ship = orbitingShips.Single(s => s == orbitingShip);
            var remainingShips = orbitingShips.Except(ship);
            ship.ReattachToParentFleetContainer();
            D.Log("{0} has left orbit around {1}.", ship.FullName, OrbitedObject.FullName);
            float shipOrbitRadius = Vector3.Distance(ship.Position, OrbitedObject.Position);
            if (!Contains(shipOrbitRadius)) {
                D.Warn("{0} orbit radius of {1} is not contained within {2}'s {3}.", ship.FullName, shipOrbitRadius, OrbitedObject.FullName, this);
            }
            if (!remainingShips.Any()) {
                _orbiter.enabled = false; // leave the orbiter object for future visitors
            }
        }

        /// <summary>
        /// Checks the position of the ship relative to the orbit slot. Returns <c>true</c> if within the orbit slot
        /// and false if not. <c>distanceToMeanRadiusOrbit</c> is a signed value indicating how far away the
        /// ship is from the orbit's mean radius. A negative value indicates that the ship is inside the mean, 
        /// positive outside.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="distanceToOrbit">A signed value indicating how far away the
        /// ship is from the orbit's mean radius. A negative value indicates that the ship is inside the mean, positive outside.</param>
        /// <returns></returns>
        public bool CheckPositionForOrbit(IShipItem ship, out float distanceToOrbit) {
            float shipDistance = Vector3.Distance(ship.Position, OrbitedObject.Position);
            distanceToOrbit = shipDistance - MeanRadius;
            if (Contains(shipDistance)) {
                return true;
            }
            //D.Log("{0}'s distance {1:0.#} from {2} is not within {2}'s {3}.", ship.FullName, shipDistance, OrbitedObject.FullName, this);
            return false;
        }

        private void OnOrbitedObjectDeath(IMortalItem orbitedObject) {
            if (onOrbitedObjectDeathOneShot != null) {
                onOrbitedObjectDeathOneShot();
                onOrbitedObjectDeathOneShot = null;
            }
            // the _orbiter will be disabled when the last ship breaks orbit
            // the _orbiter GO will be destroyed when its parent OrbitedObject is destroyed
        }

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

