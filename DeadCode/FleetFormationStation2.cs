// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetFormationStation2.cs
// Formation station for a ship in a Fleet formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Formation station for a ship in a Fleet formation.
    /// </summary>
    [System.Obsolete]
    public class FleetFormationStation2 : INavigableTarget {

        private static string _nameFormat = "{0}.{1}";

        /// <summary>
        /// Indicates whether the assignedShip is on its formation station.
        /// <remarks>The ship is OnStation if its entire CollisionDetectionZone is 
        /// within the FormationStation 'sphere' defined by Radius.
        /// </remarks>
        /// </summary>
        public bool IsOnStation { get { return VectorToStation.magnitude + _assignedShip.CollisionDetectionZoneRadius < Radius; } }

        public Vector3 StationOffset { get; set; }

        private IShipItem _assignedShip;
        private IUnitCmdItem _fleetCmd;

        /// <summary>
        /// The vector from the currently assigned ship to the station.
        /// </summary>
        public Vector3 VectorToStation { get { return Position - _assignedShip.Position; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetFormationStation2"/> class.
        /// </summary>
        /// <param name="assignedShip">The assigned ship.</param>
        /// <param name="stationOffset">The station offset.</param>
        public FleetFormationStation2(IShipItem assignedShip, Vector3 stationOffset) {
            _assignedShip = assignedShip;
            _fleetCmd = assignedShip.Command;
            StationOffset = stationOffset;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region INavigableTarget Members

        public string DisplayName { get { return _nameFormat.Inject(_assignedShip.DisplayName, GetType().Name); } }

        public string FullName { get { return _nameFormat.Inject(_assignedShip.FullName, GetType().Name); } }

        public bool IsMobile { get { return true; } }

        public Vector3 Position { get { return _fleetCmd.Position + StationOffset; } }

        public float Radius { get { return TempGameValues.FleetFormationStationRadius; } }

        public float RadiusAroundTargetContainingKnownObstacles { get { return Constants.ZeroF; } }

        public Topography Topography { get { return References.SectorGrid.GetSpaceTopography(Position); } }

        public float GetShipArrivalDistance(float shipCollisionDetectionRadius) {
            D.Assert(_assignedShip.CollisionDetectionZoneRadius.ApproxEquals(shipCollisionDetectionRadius));   // its the same ship
            // entire shipCollisionDetectionZone is within the FormationStation 'sphere' defined by Radius
            return Radius - shipCollisionDetectionRadius;
        }

        #endregion


    }
}

