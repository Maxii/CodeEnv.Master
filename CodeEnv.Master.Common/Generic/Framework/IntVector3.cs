// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntVector3.cs
// Immutable Vector3 struct using integers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Immutable Vector3 struct using integers.
    /// </summary>
    [Serializable]
    public struct IntVector3 : IEquatable<IntVector3>, IComparable<IntVector3> {

        public static readonly IntVector3 One = new IntVector3(1, 1, 1);

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(IntVector3 left, IntVector3 right) {
            return left.Equals(right);
        }

        public static bool operator !=(IntVector3 left, IntVector3 right) {
            return !left.Equals(right);
        }

        #endregion

        #region Arithmetic Operators Override

        public static IntVector3 operator +(IntVector3 left, IntVector3 right) {
            return new IntVector3(left.x + right.x, left.y + right.y, left.z + right.z);
        }

        public static IntVector3 operator -(IntVector3 left, IntVector3 right) {
            return new IntVector3(left.x - right.x, left.y - right.y, left.z - right.z);
        }

        public static IntVector3 operator *(IntVector3 left, int scaler) {
            return new IntVector3(left.x * scaler, left.y * scaler, left.z * scaler);
        }

        public static IntVector3 operator *(int scaler, IntVector3 right) {
            return new IntVector3(right.x * scaler, right.y * scaler, right.z * scaler);
        }

        #endregion

        #region Conversion Operators Override

        public static explicit operator Vector3(IntVector3 int3) {
            return new Vector3(int3.x, int3.y, int3.z);
        }

        #endregion

        private static string _toStringFormat = "({0},{1},{2})";

        public float SqrMagnitude { get { return Vector3.Dot((Vector3)this, (Vector3)this); } }

        public readonly int x;
        public readonly int y;
        public readonly int z;

        public IntVector3(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is IntVector3)) { return false; }
            return Equals((IntVector3)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + x.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + y.GetHashCode();
                hash = hash * 31 + z.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(x, y, z);
        }

        #region IEquatable<Index3D> Members

        public bool Equals(IntVector3 other) {
            return x == other.x && y == other.y && z == other.z;
        }

        #endregion

        #region IComparable<Index3D> Members

        public int CompareTo(IntVector3 other) {
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

