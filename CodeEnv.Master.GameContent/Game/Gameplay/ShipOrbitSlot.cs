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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Ship orbit slots around other bodies that knows how to place
    /// and remove a ship into/from orbit.
    /// </summary>
    public class ShipOrbitSlot : AOrbitSlot {

        public IShipOrbitable OrbitedObject { get; private set; }

        private IShipOrbitSimulator _orbitSimulator;
        private IList<IShipItem> _orbitingShips;

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
            _orbitingShips = new List<IShipItem>();
        }


        /// <summary>
        /// Notifies the ShipOrbitSlot that the provided ship is preparing to assume orbit around <c>OrbitedObject</c>.
        /// Returns the OrbitSimulator for the ship to attach too.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <returns></returns>
        public IShipOrbitSimulator PrepareToAssumeOrbit(IShipItem ship) {
            if (_orbitSimulator == null) {
                D.Assert(_orbitingShips.Count == Constants.Zero);
                GameObject orbitedObjectGo = OrbitedObject.Transform.gameObject;
                _orbitSimulator = References.GeneralFactory.MakeOrbitSimulatorInstance(orbitedObjectGo, _isOrbitedObjectMobile, true, _orbitPeriod, "ShipOrbitSimulator") as IShipOrbitSimulator;
            }
            _orbitingShips.Add(ship);
            _orbitSimulator.IsActive = true;
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
        public void OnLeftOrbit(IShipItem ship) {
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
                _orbitSimulator.IsActive = false;
                D.Log("Destroying {0}'s {1}.", OrbitedObject.FullName, typeof(IOrbitSimulator).Name);
                GameObject.Destroy(_orbitSimulator.Transform.gameObject);
                // IMPROVE could also keep it around for future uses as rigidbody.isKinematic = true so not using up physics engine cycles
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

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

