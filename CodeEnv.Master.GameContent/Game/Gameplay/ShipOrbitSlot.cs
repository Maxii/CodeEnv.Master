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

    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Ship orbit slots around other bodies that knows how to place
    /// and remove a ship into/from orbit.
    /// </summary>
    public class ShipOrbitSlot : AOrbitSlot {

        private Transform _orbiterTransform;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot"/> class.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        public ShipOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile)
            : base(innerRadius, outerRadius, isOrbitedObjectMobile) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrbitSlot"/> class.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is object being orbited mobile].</param>
        /// <param name="orbitedObject">The object being orbited.</param>
        public ShipOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile, GameObject orbitedObject)
            : base(innerRadius, outerRadius, isOrbitedObjectMobile, orbitedObject) {
        }

        public void AssumeOrbit(IShipModel ship) {
            if (_orbiterTransform == null) {
                GameObject orbiterGo = References.GeneralFactory.MakeOrbiterInstance(OrbitedObject, _isOrbitedObjectMobile, isForShips: true, name: "");
                _orbiterTransform = orbiterGo.transform;
            }
            ship.Transform.parent = _orbiterTransform;    // ship retains existing position, rotation, scale and layer
            _orbiterTransform.GetInterface<IOrbiterForShips>().enabled = true;
            float shipOrbitRadius = Vector3.Distance(ship.Transform.position, OrbitedObject.transform.position);
            if (!Contains(shipOrbitRadius)) {
                D.Warn("{0} orbit radius of {1} is not contained within {2}'s {3}.", ship.FullName, shipOrbitRadius, OrbitedObject.name, this);
            }
        }

        public void LeaveOrbit(IShipModel orbitingShip) {
            D.Assert(_orbiterTransform != null);
            var orbitingShips = _orbiterTransform.gameObject.GetSafeInterfacesInChildren<IShipModel>();
            var ship = orbitingShips.Single(s => s == orbitingShip);
            var remainingShips = orbitingShips.Except(ship);
            var parentFleetTransform = ship.Command.Transform.parent;
            ship.Transform.parent = parentFleetTransform;
            if (!remainingShips.Any()) {
                _orbiterTransform.GetInterface<IOrbiterForShips>().enabled = false; // leave the orbiter object for future visitors
            }
        }

        public override string ToString() {
            return "{0} [{1}-{2}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

