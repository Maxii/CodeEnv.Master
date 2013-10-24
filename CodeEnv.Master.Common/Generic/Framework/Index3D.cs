// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Index3D.cs
// General container class holding 3 ints.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using System;
namespace CodeEnv.Master.Common {

    /// <summary>
    /// General container class holding 3 ints.
    /// </summary>
    public class Index3D : IEquatable<Index3D> {

        public int X { get; private set; }

        public int Y { get; private set; }

        public int Z { get; private set; }

        public Index3D() : this(1, 1, 1) { }

        public Index3D(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }


        // Override object.Equals on reference types when you do not want your
        // reference type to obey reference semantics, as defined by System.Object.
        // Always override ValueType.Equals for your own Value Types.
        public override bool Equals(object right) {
            // TODO the containing class T must extend IEquatable<T>
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238 aka
            // "Rarely override the operator==() when you create reference types as
            // the .NET Framework classes expect it to follow reference semantics for
            // all reference types. Always override the == operator for your own
            // Value Types. See Effective C#, Item 6.

            // No need to check 'this' for null as the CLR throws an exception before
            // calling any instance method through a null reference.
            if (object.ReferenceEquals(right, null)) {
                return false;
            }

            if (object.ReferenceEquals(this, right)) {
                return true;
            }

            if (this.GetType() != right.GetType()) {
                return false;
            }

            // now call IEquatable's Equals
            return this.Equals(right as Index3D);
        }

        // Generally, do not override object.GetHashCode as object's version is reliable
        // although not efficient. You should override it IFF operator==() is redefined which
        // is rare. 
        // You should always override ValueType.GetHashCode and redefine ==() for your
        // value types. If the value type is used as a hash key, it must be immutable.
        // See Effective C# Item 7.
        public override int GetHashCode() {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }

        public override string ToString() {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        #region IEquatable<Index3D> Members
        public bool Equals(Index3D other) {
            // TODO add your equality test here. Call the base class Equals only if the
            // base class version is not provided by System.Object or System.ValueType
            // as all that occurs is either a check for reference equality or content equality.
            return X == other.X && Y == other.Y && Z == other.Z;
        }
        #endregion

    }
}

