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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for orbit slots that know how to place an object into orbit around a Celestial object.
    /// These orbit slots are currently used to create orbits in Systems (aka 'around' a star) and around planets.
    /// </summary>
    [System.Obsolete]
    public class CelestialOrbitSlot : AOrbitSlot {

        public GameObject OrbitedObject { get; private set; }

        public IOrbitSimulator OrbitSimulator { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CelestialOrbitSlot" /> class.
        /// WARNING: The orbiter and all its children (the actual orbiting object) will assume the layer of the orbitedObject.
        /// </summary>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="outerRadius">The outer radius.</param>
        /// <param name="orbitedObject">The GameObject being orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="toOrbit">if set to <c>true</c> the orbit simulator will rotate if activated.</param>
        public CelestialOrbitSlot(float innerRadius, float outerRadius, GameObject orbitedObject, bool isOrbitedObjectMobile, GameTimeDuration orbitPeriod, bool toOrbit = true)
            : base(innerRadius, outerRadius, isOrbitedObjectMobile, orbitPeriod, toOrbit) {
            OrbitedObject = orbitedObject;
        }

        /// <summary>
        /// The orbitingObject assumes an orbit around the preset OrbitedObject,
        /// beginning at a random point on the meanRadius of this orbit slot. 
        /// </summary>
        /// <param name="orbitingObject">The object that wants to assume an orbit.</param>
        /// <returns></returns>
        //public void AssumeOrbit(Transform orbitingObject) {
        //    D.Log("{0}.AssumeOrbit({1}) called.", OrbitedObject.name, orbitingObject.name);
        //    OrbitSimulator = References.GeneralFactory.InstallCelestialObjectInOrbit(orbitingObject.gameObject, this);
        //    orbitingObject.localPosition = GenerateRandomLocalPositionWithinSlot();
        //}

        /// <summary>
        /// Destroys the orbit simulator referenced by this CelestialOrbitSlot once the
        /// simulator has no more children.
        /// </summary>
        //public void DestroyOrbitSimulator() {
        //    D.Assert(OrbitSimulator != null, "Attempting to destroy a non-existant {0} around {1}.".Inject(typeof(IOrbitSimulator).Name, OrbitedObject.name));
        //    WaitJobUtility.WaitWhileCondition(() => OrbitSimulator.transform.childCount > Constants.Zero, onWaitFinished: (jobWasKilled) => {
        //        GameUtility.DestroyIfNotNullOrAlreadyDestroyed<IOrbitSimulator>(OrbitSimulator);
        //        OrbitSimulator = null;
        //        D.Log("{0} around {1} destroyed.", typeof(IOrbitSimulator).Name, OrbitedObject.name);
        //    });
        //}

        ///// <summary>
        ///// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
        ///// Use to set the local position of the orbiting object once attached to the orbiter.
        ///// </summary>
        ///// <returns></returns>
        //private Vector3 GenerateRandomLocalPositionWithinSlot() {
        //    Vector2 pointOnCircle = RandomExtended.PointOnCircle(MeanRadius);
        //    return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
        //}

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

