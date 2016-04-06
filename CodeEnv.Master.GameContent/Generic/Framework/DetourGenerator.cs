// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DetourGenerator.cs
// General purpose generator of ship detours used by Items that implement IAvoidableObstacle.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// General purpose generator of ship detours used by Items that implement IAvoidableObstacle.
    /// </summary>
    public class DetourGenerator {

        private Reference<Vector3> _obstacleZoneCenterRef;
        private Vector3 _obstacleZoneCenter;
        /// <summary>
        /// The center of the obstacle zone in world space.
        /// </summary>
        private Vector3 ObstacleZoneCenter {
            get {
                if (_obstacleZoneCenterRef != null) {
                    return _obstacleZoneCenterRef.Value;
                }
                return _obstacleZoneCenter;
            }
        }

        private float _obstacleZoneRadius;
        private float _distanceToClearObstacle;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetourGenerator"/> class.
        /// </summary>
        /// <param name="obstacleZoneCenter">The center of the mobile AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        public DetourGenerator(Reference<Vector3> obstacleZoneCenter, float obstacleZoneRadius, float distanceToClearObstacle) {
            _obstacleZoneCenterRef = obstacleZoneCenter;
            _obstacleZoneRadius = obstacleZoneRadius;
            _distanceToClearObstacle = distanceToClearObstacle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DetourGenerator"/> class.
        /// </summary>
        /// <param name="obstacleZoneCenter">The center of the stationary AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        public DetourGenerator(Vector3 obstacleZoneCenter, float obstacleZoneRadius, float distanceToClearObstacle) {
            _obstacleZoneCenter = obstacleZoneCenter;
            _obstacleZoneRadius = obstacleZoneRadius;
            _distanceToClearObstacle = distanceToClearObstacle;
        }

        /// <summary>
        /// Generates a detour based on where the Obstacle's AvoidableObstacleZone was hit. The resulting detour
        /// will be located outside the zone and close to the zone hit point.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The zone hit pt.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourFromObstacleZoneHit(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float fleetRadius) {
            Vector3 ptOnZonePerimeterOnWayToDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, _obstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToDetourFromZoneCenter = (ptOnZonePerimeterOnWayToDetour - ObstacleZoneCenter).normalized;
            float distanceToDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 detour = ObstacleZoneCenter + directionToDetourFromZoneCenter * distanceToDetourFromZoneCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour directly above or below the poles of the obstacle.
        /// Which pole is determined by the position of the ship at the time of the hit.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAtObstaclePoles(Vector3 shipOrFleetPosition, float fleetRadius) {
            // Very simple: if below plane go below down pole, if above go above up pole
            float centerPlaneY = ObstacleZoneCenter.y;

            bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
            Vector3 directionToDetourFromZoneCenter = isShipOrFleetOnOrAbovePlane ? Vector3.up : Vector3.down;

            float distanceToDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 detour = ObstacleZoneCenter + directionToDetourFromZoneCenter * distanceToDetourFromZoneCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour around the poles of the obstacle based on where the
        /// Obstacle's AvoidableObstacleZone was hit. The resulting detour will be located outside the
        /// zone and close to the zone hit point, but always above or below the Y value of the poles.
        /// Which pole is determined by the position of the ship at the time of the hit.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The zone hit pt.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAroundPolesFromZoneHit(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float fleetRadius) {
            Vector3 ptOnZonePerimeterOnWayToInitialDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, ObstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToInitialDetourFromZoneCenter = (ptOnZonePerimeterOnWayToInitialDetour - ObstacleZoneCenter).normalized;
            float distanceToInitialHQDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 initialDetour = ObstacleZoneCenter + directionToInitialDetourFromZoneCenter * distanceToInitialHQDetourFromZoneCenter;

            float centerPlaneY = ObstacleZoneCenter.y;
            float desiredClearanceFromPlane = _distanceToClearObstacle + fleetRadius;
            float initialDetourYRelativeToPlane = initialDetour.y - centerPlaneY;
            // place detour above or below plane
            float finalDetourYRelativeToPlane = initialDetourYRelativeToPlane;
            if (Mathfx.Approx(initialDetourYRelativeToPlane, Constants.ZeroF, .01F)) {
                // initialDetour is right on plane so finalDetour placement above or below plane determined by shipOrFleetPosition
                bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
                finalDetourYRelativeToPlane += isShipOrFleetOnOrAbovePlane ? desiredClearanceFromPlane : -desiredClearanceFromPlane;
            }
            else if (initialDetourYRelativeToPlane > Constants.ZeroF) {
                // initialDetour is above plane
                finalDetourYRelativeToPlane += desiredClearanceFromPlane;
            }
            else {
                // initialDetour is below plane
                finalDetourYRelativeToPlane -= desiredClearanceFromPlane;
            }

            // avoid going above or below the plane more than needed
            if (finalDetourYRelativeToPlane > Constants.ZeroF) {
                finalDetourYRelativeToPlane = Mathf.Min(finalDetourYRelativeToPlane, desiredClearanceFromPlane);
            }
            else {        // can't be == 0 as has to be either above or below
                finalDetourYRelativeToPlane = Mathf.Max(finalDetourYRelativeToPlane, -desiredClearanceFromPlane);
            }
            float finalDetourY = centerPlaneY + finalDetourYRelativeToPlane;
            Vector3 finalDetour = initialDetour.SetY(finalDetourY);
            return finalDetour;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

