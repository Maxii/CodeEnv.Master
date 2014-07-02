// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CelestialOrbitSlot.cs
// Class for Celestial orbit slots that know how to place a Celestial object into orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Celestial orbit slots that know how to place a Celestial 
    /// object into orbit.
    /// </summary>
    public class CelestialOrbitSlot : AOrbitSlot {

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitalSlot" /> struct.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        public CelestialOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile)
            : base(innerRadius, outerRadius, isOrbitedObjectMobile) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CelestialOrbitSlot"/> class.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is object being orbited mobile].</param>
        /// <param name="orbitedObject">The object being orbited.</param>
        public CelestialOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile, GameObject orbitedObject) : base(innerRadius, outerRadius, isOrbitedObjectMobile, orbitedObject) { }

        /// <summary>
        /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
        /// Use to set the local position of the orbiting object once attached to the orbiter.
        /// </summary>
        /// <returns></returns>
        private Vector3 GenerateRandomLocalPositionWithinSlot() {
            Vector2 pointOnCircle = RandomExtended<Vector2>.OnCircle(MeanRadius);
            return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
        }

        public void AssumeOrbit(Transform celestialObject) {
            D.Assert(celestialObject.GetInterface<IShipModel>() == null);
            D.Assert(celestialObject.parent == null, "{0} should not have parent {1}.".Inject(celestialObject.name, celestialObject.parent.name));
            GameObject orbiterGo = References.GeneralFactory.MakeOrbiterInstance(OrbitedObject, _isOrbitedObjectMobile, isForShips: false, name: "");
            UnityUtility.AttachChildToParent(celestialObject.gameObject, orbiterGo);
            celestialObject.localPosition = GenerateRandomLocalPositionWithinSlot();
        }

        public override string ToString() {
            return "{0} [{1}-{2}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

