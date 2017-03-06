// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StationaryLocation.cs
// An INavigableTarget wrapping a stationary location in world space.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An INavigable target wrapping a stationary location in world space.
    /// </summary>
    /// <seealso cref="CodeEnv.Master.GameContent.INavigable" />
    /// <seealso cref="System.IEquatable{CodeEnv.Master.GameContent.StationaryLocation}" />
    public struct StationaryLocation : IShipNavigable, IFleetNavigable, IEquatable<StationaryLocation> {

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
            Topography = References.GameManager.GameKnowledge.GetSpaceTopography(position);
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
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Position.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Topography.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<StationaryLocation> Members

        public bool Equals(StationaryLocation other) {
            return Position == other.Position && Topography == other.Topography;
        }

        #endregion

        #region INavigable Members

        public string Name { get { return DebugName; } }

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = "{0}[{1}]".Inject(GetType().Name, Position);
                }
                return _debugName;
            }
        }

        public Vector3 Position { get; private set; }

        public bool IsMobile { get { return false; } }

        public bool IsOperational { get { return true; } }

        #endregion

        #region IShipNavigable Members

        public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
            return new ApMoveDestinationProxy(this, ship, tgtOffset, Constants.ZeroF, TempGameValues.WaypointCloseEnoughDistance);
        }

        #endregion

        #region IFleetNavigable Members

        public Topography Topography { get; private set; }

        public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
            return Vector3.Distance(fleetPosition, Position);
        }


        #endregion

    }

    #region Class Version Archive

    ///// <summary>
    ///// An INavigableTarget wrapping a stationary location in world space.
    ///// <remarks>Changed to class from struct to allow inheritance. Retains value semantics - 3.12.16</remarks>
    ///// </summary>
    //public class StationaryLocation : INavigableTarget, IEquatable<StationaryLocation> {

    //    #region Equality Operators Override

    //    // see C# 4.0 In a Nutshell, page 254

    //    public static bool operator ==(StationaryLocation left, StationaryLocation right) {
    //        return left.Equals(right);
    //    }

    //    public static bool operator !=(StationaryLocation left, StationaryLocation right) {
    //        return !left.Equals(right);
    //    }

    //    #endregion

    //    public StationaryLocation(Vector3 position) {
    //        Position = position;
    //        Topography = References.SectorGrid.GetSpaceTopography(position);
    //    }

    //    #region Object.Equals and GetHashCode Override

    //    public override bool Equals(object obj) {
    //        if (!(obj is StationaryLocation)) { return false; }
    //        return Equals((StationaryLocation)obj);
    //    }

    //    /// <summary>
    //    /// Returns a hash code for this instance.
    //    /// See Page 254, C# 4.0 in a Nutshell.
    //    /// </summary>
    //    /// <returns>
    //    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    //    /// </returns>
    //    public override int GetHashCode() {
    //        int hash = 17;  // 17 = some prime number
    //        hash = hash * 31 + Position.GetHashCode(); // 31 = another prime number
    //        hash = hash * 31 + Topography.GetHashCode();
    //        return hash;
    //    }

    //    #endregion

    //    public override string ToString() {
    //        return DebugName;
    //    }

    //    #region IEquatable<StationaryLocation> Members

    //    public bool Equals(StationaryLocation other) {
    //        return Position == other.Position && Topography == other.Topography;
    //    }

    //    #endregion

    //    #region INavigableTarget Members

    //    public string DisplayName { get { return DebugName; } }

    //    public string DebugName { get { return string.Format("{0}[{1}]", GetType().Name, Position); } }

    //    public Vector3 Position { get; private set; }

    //    public bool IsMobile { get { return false; } }

    //    public float Radius { get { return Constants.ZeroF; } }

    //    public Topography Topography { get; private set; }

    //    public float RadiusAroundTargetContainingKnownObstacles { get { return Constants.ZeroF; } }

    //    public float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
    //        return TempGameValues.WaypointCloseEnoughDistance;
    //    }

    //    #endregion

    //}


    #endregion

}

