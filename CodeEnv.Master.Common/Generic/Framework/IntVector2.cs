// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntVector2.cs
// Immutable Vector2 struct using integers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Immutable Vector2 struct using integers.
    /// </summary>
    [Serializable]
    public struct IntVector2 : IEquatable<IntVector2>, IComparable<IntVector2> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(IntVector2 left, IntVector2 right) {
            return left.Equals(right);
        }

        public static bool operator !=(IntVector2 left, IntVector2 right) {
            return !left.Equals(right);
        }

        #endregion

        #region Arithmetic Operators Override

        public static IntVector2 operator +(IntVector2 left, IntVector2 right) {
            return new IntVector2(left.x + right.x, left.y + right.y);
        }

        public static IntVector2 operator -(IntVector2 left, IntVector2 right) {
            return new IntVector2(left.x - right.x, left.y - right.y);
        }

        public static IntVector2 operator *(IntVector2 left, int scaler) {
            return new IntVector2(left.x * scaler, left.y * scaler);
        }

        public static IntVector2 operator *(int scaler, IntVector2 right) {
            return new IntVector2(right.x * scaler, right.y * scaler);
        }

        #endregion

        #region Conversion Operators Override

        public static explicit operator Vector2(IntVector2 int2) {
            return new Vector2(int2.x, int2.y);
        }

        #endregion

        private const string ToStringFormat = "({0},{1})";

        public float SqrMagnitude { get { return Vector2.Dot((Vector2)this, (Vector2)this); } }

        public readonly int x;
        public readonly int y;

        public IntVector2(int x, int y) {
            this.x = x;
            this.y = y;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is IntVector2)) { return false; }
            return Equals((IntVector2)obj);
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
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return ToStringFormat.Inject(x, y);
        }

        #region IEquatable<IntVector2> Members

        public bool Equals(IntVector2 other) {
            return x == other.x && y == other.y;
        }

        #endregion

        #region IComparable<IntVector2> Members

        public int CompareTo(IntVector2 other) {
            // orders by x, then y
            var xResult = this.x.CompareTo(other.x);
            if (xResult != Constants.Zero) {
                return xResult;
            }
            else {
                return this.y.CompareTo(other.y);
            }
        }

        #endregion
    }
}

