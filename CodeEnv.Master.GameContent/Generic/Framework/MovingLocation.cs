// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MovingLocation.cs
// An INavigableTarget wrapping a moving location.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An INavigableTarget wrapping a moving location.
    /// </summary>
    public class MovingLocation : INavigableTarget {

        private Reference<Vector3> _movingPosition;

        public MovingLocation(Reference<Vector3> movingPosition) {
            _movingPosition = movingPosition;
        }

        public override string ToString() {
            return FullName;
        }

        #region INavigableTarget Members

        public string DisplayName { get { return FullName; } }

        public string FullName { get { return string.Format("{0}[{1}]", GetType().Name, Position); } }

        public Vector3 Position { get { return _movingPosition.Value; } }

        public bool IsMobile { get { return true; } }

        public float Radius { get { return Constants.ZeroF; } }

        public Topography Topography { get { return References.SectorGrid.GetSpaceTopography(Position); } }

        public float RadiusAroundTargetContainingKnownObstacles { get { return Constants.ZeroF; } }

        public float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
            return TempGameValues.WaypointCloseEnoughDistance;
        }

        #endregion

    }
}

