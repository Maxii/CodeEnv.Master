// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CelestialOrbitSlot.cs
// Class for orbit slots that know how to place an object into orbit around a Celestial object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for orbit slots that know how to place an object into orbit around a Celestial object.
    /// These orbit slots are currently used to create orbits in Systems (aka 'around' a star) and 
    /// around planets.
    /// </summary>
    public class CelestialOrbitSlot : AOrbitSlot {

        private GameObject _orbitedObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="CelestialOrbitSlot" /> class. Convenience constructor 
        /// if you want the orbiter created with AssumeOrbit() to directly parent itself to this IModel gameObject. 
        /// WARNING: The orbiter and all its children (the actual orbiting object) will assume the layer of the IModel 
        /// gameobject. If you want control over the layer assumed, use the alternative constructor and substitute
        /// another gameObject for the IModel.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="orbitedModel">The IModel being orbited.</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public CelestialOrbitSlot(float innerRadius, float outerRadius, IModel orbitedModel, GameTimeDuration orbitPeriod)
            : this(innerRadius, outerRadius, orbitedModel.Transform.gameObject, orbitedModel.IsMobile, orbitPeriod) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CelestialOrbitSlot"/> class.
        /// WARNING: The orbiter and all its children (the actual orbiting object) will assume the layer of the orbitedObject.
        /// </summary>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="outerRadius">The outer radius.</param>
        /// <param name="orbitedObject">The GameObject being orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public CelestialOrbitSlot(float innerRadius, float outerRadius, GameObject orbitedObject, bool isOrbitedObjectMobile, GameTimeDuration orbitPeriod)
            : base(innerRadius, outerRadius, isOrbitedObjectMobile, orbitPeriod) {
            _orbitedObject = orbitedObject;
        }

        /// <summary>
        /// The orbitingObject assumes an orbit around the preset OrbitedObject,
        /// beginning at a random point on the meanRadius of this orbit slot. Returns the newly instantiated
        /// IOrbiter, parented to the OrbitedObject and the parent of <c>orbitingObject</c>.
        /// </summary>
        /// <param name="orbitingObject">The object that wants to assume an orbit.</param>
        /// <param name="orbiterName">Name of the <c>Orbiter</c> object created to simulate orbit movement.</param>
        /// <returns></returns>
        public IOrbiter AssumeOrbit(Transform orbitingObject, string orbiterName = "") {
            D.Assert(orbitingObject.GetInterface<IShipModel>() == null);
            IOrbiter orbiter = References.GeneralFactory.MakeOrbiterInstance(_orbitedObject, _isOrbitedObjectMobile, false, _orbitPeriod, orbiterName: orbiterName);
            UnityUtility.AttachChildToParent(orbitingObject.gameObject, orbiter.Transform.gameObject);
            orbitingObject.localPosition = GenerateRandomLocalPositionWithinSlot();
            return orbiter;
        }

        /// <summary>
        /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
        /// Use to set the local position of the orbiting object once attached to the orbiter.
        /// </summary>
        /// <returns></returns>
        private Vector3 GenerateRandomLocalPositionWithinSlot() {
            Vector2 pointOnCircle = RandomExtended<Vector2>.OnCircle(MeanRadius);
            return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
        }

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

