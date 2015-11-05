// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Index3D.cs
// Immutable location struct holding 3 ints.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Immutable location struct holding 3 ints.
    /// </summary>
    public struct Index3D : IEquatable<Index3D>, IComparable<Index3D> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(Index3D left, Index3D right) {
            return left.Equals(right);
        }

        public static bool operator !=(Index3D left, Index3D right) {
            return !left.Equals(right);
        }

        #endregion

        private static string _toStringFormat = "({0},{1},{2})";

        public readonly int x;
        public readonly int y;
        public readonly int z;

        public Index3D(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is Index3D)) { return false; }
            return Equals((Index3D)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + x.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + y.GetHashCode();
            hash = hash * 31 + z.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(x, y, z);
        }

        #region IEquatable<Index3D> Members

        public bool Equals(Index3D other) {
            return x == other.x && y == other.y && z == other.z;
        }

        #endregion

        #region IComparable<Index3D> Members

        public int CompareTo(Index3D other) {
            // orders by x, then y, then z
            var xResult = this.x.CompareTo(other.x);
            if (xResult != Constants.Zero) {
                return xResult;
            }
            else {
                var yResult = this.y.CompareTo(other.y);
                if (yResult != Constants.Zero) {
                    return yResult;
                }
                else {
                    return this.z.CompareTo(other.z);
                }
            }
        }

        #endregion
    }
}

