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

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Structure that describes key characteristics of an orbit around an object. 
    /// Also can generate a random position within the slot for the orbiter to start orbiting.
    /// </summary>
    public struct OrbitalSlot {

        #region Equality Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OrbitalSlot left, OrbitalSlot right) {
            return left.Equals(right);
        }

        public static bool operator !=(OrbitalSlot left, OrbitalSlot right) {
            return !left.Equals(right);
        }

        #endregion

        public float MinimumDistance { get; private set; }

        public float MaximumDistance { get; private set; }

        public float MeanDistance { get; private set; }

        public OrbitalSlot(float minimumRadius, float maximumRadius)
            : this() {
            Arguments.Validate(minimumRadius != maximumRadius);
            Arguments.ValidateForRange(minimumRadius, Constants.ZeroF, maximumRadius);
            Arguments.ValidateForRange(maximumRadius, minimumRadius, Mathf.Infinity);
            MinimumDistance = minimumRadius;
            MaximumDistance = maximumRadius;
            MeanDistance = minimumRadius + (maximumRadius - minimumRadius) / 2F;
        }

        public Vector3 GenerateRandomPositionWithinSlot() {
            Vector2 pointOnCircle = RandomExtended<Vector2>.OnCircle(MeanDistance);
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
            hash = hash * 31 + MinimumDistance.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + MaximumDistance.GetHashCode();
            return hash;
        }

        #endregion

        #region IEquatable<OrbitalSlot> Members

        public bool Equals(OrbitalSlot other) {
            return MinimumDistance == other.MinimumDistance && MaximumDistance == other.MaximumDistance;
        }

        #endregion

        public override string ToString() {
            return "{0} [{1} - {2}]".Inject(GetType().Name, MinimumDistance, MaximumDistance);
        }

    }
}

