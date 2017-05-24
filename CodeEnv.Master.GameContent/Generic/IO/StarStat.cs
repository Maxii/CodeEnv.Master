// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarStat.cs
// Immutable struct containing externally acquirable values for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Stars.
    /// </summary>
    public struct StarStat : IEquatable<StarStat> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(StarStat left, StarStat right) {
            return left.Equals(right);
        }

        public static bool operator !=(StarStat left, StarStat right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get { return GetType().Name; } }

        // a Star's name is assigned when its parent system becomes known
        public StarCategory Category { get; private set; }
        public float Radius { get; private set; }
        public float CloseOrbitInnerRadius { get; private set; }
        public int Capacity { get; private set; }
        public ResourceYield Resources { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarStat" /> struct.
        /// </summary>
        /// <param name="category">The category of Star.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="closeOrbitInnerRadius">The low orbit radius.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="resources">The resources.</param>
        public StarStat(StarCategory category, float radius, float closeOrbitInnerRadius, int capacity, ResourceYield resources)
            : this() {
            Category = category;
            Radius = radius;
            CloseOrbitInnerRadius = closeOrbitInnerRadius;
            Capacity = capacity;
            Resources = resources;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is StarStat)) { return false; }
            return Equals((StarStat)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;
                hash = hash * 31 + Category.GetHashCode();
                hash = hash * 31 + Radius.GetHashCode();
                hash = hash * 31 + CloseOrbitInnerRadius.GetHashCode();
                hash = hash * 31 + Capacity.GetHashCode();
                hash = hash * 31 + Resources.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<StarStat> Members

        public bool Equals(StarStat other) {
            return Category == other.Category && Radius == other.Radius && CloseOrbitInnerRadius == other.CloseOrbitInnerRadius
                && Capacity == other.Capacity && Resources == other.Resources;
        }

        #endregion

    }
}

