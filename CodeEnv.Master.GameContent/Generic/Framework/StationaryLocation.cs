// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StationaryLocation.cs
// A dummy IDestinationTarget wrapping a stationary location.
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
    /// A dummy IDestinationTarget wrapping a stationary location.
    /// </summary>
    public struct StationaryLocation : IDestinationTarget, IEquatable<StationaryLocation> {

        #region Equality Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(StationaryLocation left, StationaryLocation right) {
            return left.Equals(right);
        }

        public static bool operator !=(StationaryLocation left, StationaryLocation right) {
            return !left.Equals(right);
        }

        #endregion

        public StationaryLocation(Vector3 position)
            : this() {
            Position = position;
            Topography = References.SectorGrid.GetSpaceTopography(position);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is StationaryLocation)) { return false; }
            return Equals((StationaryLocation)obj);
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
            hash = hash * 31 + Position.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Topography.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return FullName;
        }

        #region IEquatable<StationaryLocation> Members

        public bool Equals(StationaryLocation other) {
            return Position == other.Position && Topography == other.Topography;
        }

        #endregion

        #region IDestinationTarget Members

        public string DisplayName { get { return FullName; } }

        public string FullName { get { return string.Format("{0}[{1}]", this.GetType().Name, Position); } }

        // OPTIMIZE consider letting this be settable so navigator's don't have to create a new one every time
        public Vector3 Position { get; private set; }

        public bool IsMobile { get { return false; } }

        public float Radius { get { return Constants.ZeroF; } }

        public Topography Topography { get; private set; }

        #endregion
    }
}

