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
    public class CelestialOrbitSlot : AOrbitSlot {

        private GameObject _orbitedObject;
        private IOrbitSimulator _orbitSimulator;

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
        /// IOrbitSimulator, parented to the OrbitedObject and the parent of <c>orbitingObject</c>.
        /// </summary>
        /// <param name="orbitingObject">The object that wants to assume an orbit.</param>
        /// <param name="orbitSimulatorName">Name of the object created to simulate orbit movement.</param>
        /// <returns></returns>
        public IOrbitSimulator AssumeOrbit(Transform orbitingObject, string orbitSimulatorName = "") {
            D.Log("{0}.AssumeOrbit({1}) called.", _orbitedObject.name, orbitingObject.name);
            D.Assert(orbitingObject.GetComponent<IShipItem>() == null, "OrbitingObject {0} can't be a ship.", orbitingObject.name);
            if (_orbitSimulator != null) {
                D.Error("{0} attempting to assume orbit around {1} which already has {2} orbiting.", orbitingObject.name, _orbitedObject.name, _orbitSimulator.transform.name);
            }
            _orbitSimulator = References.GeneralFactory.MakeOrbitSimulatorInstance(_orbitedObject, IsOrbitedObjectMobile, false, _orbitPeriod, orbitSimulatorName);
            UnityUtility.AttachChildToParent(orbitingObject.gameObject, _orbitSimulator.transform.gameObject);
            orbitingObject.localPosition = GenerateRandomLocalPositionWithinSlot();
            return _orbitSimulator;
        }

        /// <summary>
        /// Destroys the orbiter object referenced by this CelestialOrbitSlot.
        /// </summary>
        public void DestroyOrbitSimulator() {
            D.Assert(_orbitSimulator != null, "Attempting to destroy a non-existant {0} around {1}.".Inject(typeof(IOrbitSimulator).Name, _orbitedObject.name));
            new Job(DestroyOrbitSimulatorWhenEmpty(), toStart: true, jobCompleted: (wasKilled) => {
                D.Log("{0} around {1} destroyed.", typeof(IOrbitSimulator).Name, _orbitedObject.name);
            });
        }

        private IEnumerator DestroyOrbitSimulatorWhenEmpty() {
            var cumTime = 0F;
            while (_orbitSimulator.transform.childCount > Constants.Zero) {
                cumTime += Time.deltaTime;
                if (cumTime > 6F) {
                    D.WarnContext(_orbitSimulator.transform, "{0} around {1} still waiting for destruction.", _orbitSimulator.transform.name, _orbitedObject.name);
                }
                yield return null;
            }
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed<IOrbitSimulator>(_orbitSimulator);
            _orbitSimulator = null;
        }

        /// <summary>
        /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
        /// Use to set the local position of the orbiting object once attached to the orbiter.
        /// </summary>
        /// <returns></returns>
        private Vector3 GenerateRandomLocalPositionWithinSlot() {
            Vector2 pointOnCircle = RandomExtended.PointOnCircle(MeanRadius);
            return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
        }

        public override string ToString() {
            return "{0} [{1:0.#}-{2:0.#}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

