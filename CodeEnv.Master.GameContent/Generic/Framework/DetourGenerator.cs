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

        protected Vector3 _obstacleZoneCenter;
        protected float _obstacleZoneRadius;
        protected float _distanceToClearObstacle;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetourGenerator"/> class.
        /// </summary>
        /// <param name="obstacleZoneCenter">The center of the AvoidableObstacleZone.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center .</param>
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
        /// <param name="formationOffset">The formation offset.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourFromObstacleZoneHit(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float fleetRadius, Vector3 formationOffset) {
            Vector3 ptOnZonePerimeterOnWayToDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, _obstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToDetourFromZoneCenter = (ptOnZonePerimeterOnWayToDetour - _obstacleZoneCenter).normalized;
            float distanceToHQDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 hqDetour = _obstacleZoneCenter + directionToDetourFromZoneCenter * distanceToHQDetourFromZoneCenter;
            return hqDetour + formationOffset;
        }

        /// <summary>
        /// Generates a detour directly above or below the poles of the obstacle.
        /// Which pole is determined by the position of the ship at the time of the hit.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <param name="formationOffset">The formation offset.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAtObstaclePoles(Vector3 shipOrFleetPosition, float fleetRadius, Vector3 formationOffset) {
            // Very simple: if below plane go below down pole, if above go above up pole
            float centerPlaneY = _obstacleZoneCenter.y;

            bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
            Vector3 directionToDetourFromZoneCenter = isShipOrFleetOnOrAbovePlane ? Vector3.up : Vector3.down;

            float distanceToHQDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 hqDetour = _obstacleZoneCenter + directionToDetourFromZoneCenter * distanceToHQDetourFromZoneCenter;
            return hqDetour + formationOffset;
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
        /// <param name="formationOffset">The formation offset.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAroundPolesFromZoneHit(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float fleetRadius, Vector3 formationOffset) {
            Vector3 ptOnZonePerimeterOnWayToInitialDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, _obstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToInitialDetourFromZoneCenter = (ptOnZonePerimeterOnWayToInitialDetour - _obstacleZoneCenter).normalized;
            float distanceToInitialHQDetourFromZoneCenter = _distanceToClearObstacle + fleetRadius;
            Vector3 initialHQDetour = _obstacleZoneCenter + directionToInitialDetourFromZoneCenter * distanceToInitialHQDetourFromZoneCenter;

            float centerPlaneY = _obstacleZoneCenter.y;
            float desiredHQClearanceFromPlane = _distanceToClearObstacle + fleetRadius;
            float initialHQDetourYRelativeToPlane = initialHQDetour.y - centerPlaneY;
            // place detour above or below plane
            float finalHQDetourYRelativeToPlane = initialHQDetourYRelativeToPlane;
            if (Mathfx.Approx(initialHQDetourYRelativeToPlane, Constants.ZeroF, .01F)) {
                // initialHQDetour is right on plane so finalDetour placement above or below plane determined by shipOrFleetPosition
                bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
                finalHQDetourYRelativeToPlane += isShipOrFleetOnOrAbovePlane ? desiredHQClearanceFromPlane : -desiredHQClearanceFromPlane;
            }
            else if (initialHQDetourYRelativeToPlane > Constants.ZeroF) {
                // initialHQDetour is above plane
                finalHQDetourYRelativeToPlane += desiredHQClearanceFromPlane;
            }
            else {
                // initialHQDetour is below plane
                finalHQDetourYRelativeToPlane -= desiredHQClearanceFromPlane;
            }

            // avoid going above or below the plane more than needed
            if (finalHQDetourYRelativeToPlane > Constants.ZeroF) {
                finalHQDetourYRelativeToPlane = Mathf.Min(finalHQDetourYRelativeToPlane, desiredHQClearanceFromPlane);
            }
            else {        // can't be == 0 as has to be either above or below
                finalHQDetourYRelativeToPlane = Mathf.Max(finalHQDetourYRelativeToPlane, -desiredHQClearanceFromPlane);
            }
            float finalHQDetourY = centerPlaneY + finalHQDetourYRelativeToPlane;
            Vector3 finalHQDetour = initialHQDetour.SetY(finalHQDetourY);
            return finalHQDetour + formationOffset;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

