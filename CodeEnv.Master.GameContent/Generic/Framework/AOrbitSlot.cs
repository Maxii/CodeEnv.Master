// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOrbitSlot.cs
// Abstract base class for Celestial and Ship orbit slots that know how to place
// (and remove if applicable) objects into orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Celestial and Ship orbit slots that know how to place
    /// (and remove if applicable) objects into orbit.
    /// </summary>
    public abstract class AOrbitSlot {

        /// <summary>
        /// The slot's closest distance from the body orbited.
        /// </summary>
        public float InnerRadius { get; private set; }

        /// <summary>
        /// The slot's furthest distance from the body orbited.
        /// </summary>
        public float OuterRadius { get; private set; }

        /// <summary>
        /// The slot's mean distance from the body orbited.
        /// </summary>
        public float MeanRadius { get; private set; }

        /// <summary>
        /// The slot's depth, aka OutsideRadius - InsideRadius.
        /// </summary>
        public float Depth { get; private set; }

        public GameObject OrbitedObject { get; set; }

        protected bool _isOrbitedObjectMobile;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitalSlot" /> struct.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        public AOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile) {
            Arguments.Validate(innerRadius != outerRadius);
            Arguments.ValidateForRange(innerRadius, Constants.ZeroF, outerRadius);
            Arguments.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity);
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            MeanRadius = innerRadius + (outerRadius - innerRadius) / 2F;
            Depth = outerRadius - innerRadius;
            _isOrbitedObjectMobile = isOrbitedObjectMobile;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CelestialOrbitSlot"/> class.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is object being orbited mobile].</param>
        /// <param name="orbitedObject">The object being orbited.</param>
        public AOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile, GameObject orbitedObject)
            : this(innerRadius, outerRadius, isOrbitedObjectMobile) {
            OrbitedObject = orbitedObject;
        }

        /// <summary>
        /// Determines whether [contains] [the specified orbit radius].
        /// </summary>
        /// <param name="orbitRadius">The orbit radius.</param>
        /// <returns></returns>
        public bool Contains(float orbitRadius) {
            return Utility.IsInRange(orbitRadius, InnerRadius, OuterRadius);
        }

    }
}

