// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitalSlot.cs
// Structure that describes key characteristics of an orbit around an object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Structure that describes key characteristics of an orbit around an object. 
    /// Also can generate a random position within the slot for the orbiter to start orbiting.
    /// </summary>
    public struct OrbitalSlot : IEquatable<OrbitalSlot> {

        #region Equality Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OrbitalSlot left, OrbitalSlot right) {
            return left.Equals(right);
        }

        public static bool operator !=(OrbitalSlot left, OrbitalSlot right) {
            return !left.Equals(right);
        }

        #endregion

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

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitalSlot"/> struct.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        public OrbitalSlot(float innerRadius, float outerRadius)
            : this() {
            Arguments.Validate(innerRadius != outerRadius);
            Arguments.ValidateForRange(innerRadius, Constants.ZeroF, outerRadius);
            Arguments.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity);
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            MeanRadius = innerRadius + (outerRadius - innerRadius) / 2F;
            Depth = outerRadius - innerRadius;
        }

        /// <summary>
        /// Determines whether [contains] [the specified orbit radius].
        /// </summary>
        /// <param name="orbitRadius">The orbit radius.</param>
        /// <returns></returns>
        public bool Contains(float orbitRadius) {
            return Utility.IsInRange(orbitRadius, InnerRadius, OuterRadius);
        }

        /// <summary>
        /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
        /// Use to set the local position of the orbiting object once attached to the orbiter.
        /// </summary>
        /// <returns></returns>
        public Vector3 GenerateRandomLocalPositionWithinSlot() {
            Vector2 pointOnCircle = RandomExtended<Vector2>.OnCircle(MeanRadius);
            return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is OrbitalSlot)) { return false; }
            return Equals((OrbitalSlot)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + InnerRadius.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + OuterRadius.GetHashCode();
            return hash;
        }

        #endregion

        #region IEquatable<OrbitalSlot> Members

        public bool Equals(OrbitalSlot other) {
            return InnerRadius == other.InnerRadius && OuterRadius == other.OuterRadius;
        }

        #endregion

        public override string ToString() {
            return "{0} [{1} - {2}]".Inject(GetType().Name, InnerRadius, OuterRadius);
        }

    }
}

