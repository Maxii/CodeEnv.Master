// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDetourGenerator.cs
// Facility-specific generator of ship IAvoidableObstacle detour locations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Facility-specific generator of ship IAvoidableObstacle detour locations.
    /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
    /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
    /// </summary>
    public class FacilityDetourGenerator : DetourGenerator {

        private Vector3 _baseFormationCenter;
        private float _baseFormationRadius;
        private float _distanceToClearBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityDetourGenerator" /> class.
        /// </summary>
        /// <param name="clientDebugName">DebugName of the client.</param>
        /// <param name="obstacleZoneCenter">The center of the mobile AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        /// <param name="baseFormationCenter">The formation center.</param>
        /// <param name="baseFormationRadius">The base formation radius.</param>
        /// <param name="distanceToClearBase">The distance to clear base.</param>
        public FacilityDetourGenerator(string clientDebugName, Reference<Vector3> obstacleZoneCenter, float obstacleZoneRadius,
            float distanceToClearObstacle, Vector3 baseFormationCenter, float baseFormationRadius, float distanceToClearBase)
            : base(clientDebugName, obstacleZoneCenter, obstacleZoneRadius, distanceToClearObstacle) {
            _baseFormationCenter = baseFormationCenter;
            _baseFormationRadius = baseFormationRadius;
            _distanceToClearBase = distanceToClearBase;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityDetourGenerator" /> class.
        /// </summary>
        /// <param name="clientDebugName">DebugName of the client.</param>
        /// <param name="obstacleZoneCenter">The center of the stationary AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        /// <param name="baseFormationCenter">The base formation center.</param>
        /// <param name="baseFormationRadius">The base formation radius.</param>
        /// <param name="distanceToClearBase">The distance to clear base.</param>
        public FacilityDetourGenerator(string clientDebugName, Vector3 obstacleZoneCenter, float obstacleZoneRadius, float distanceToClearObstacle,
            Vector3 baseFormationCenter, float baseFormationRadius, float distanceToClearBase)
            : base(clientDebugName, obstacleZoneCenter, obstacleZoneRadius, distanceToClearObstacle) {
            _baseFormationCenter = baseFormationCenter;
            _baseFormationRadius = baseFormationRadius;
            _distanceToClearBase = distanceToClearBase;
        }

        /// <summary>
        /// Generates a detour location in one of the 4 XZ quadrants around the belt of the base.
        /// The resulting detour will be located outside the operational perimeter of the base, 
        /// aka outside the formation and its close orbit slot. Which quadrant is determined by the position of the ship or fleet.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship or fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle
        /// at or around the poles.
        /// Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAtBaseBelt(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            // Very simple: if below plane go below down pole, if on or above go above up pole
            float centerPlaneX = _baseFormationCenter.x;
            float centerPlaneZ = _baseFormationCenter.z;

            bool isShipOrFleetOnOrRightOfXPlane = shipOrFleetPosition.x - centerPlaneX >= Constants.ZeroF;
            bool isShipOrFleetOnOrFwdOfZPlane = shipOrFleetPosition.z - centerPlaneZ >= Constants.ZeroF;

            Vector3 xDirectionToDetourFromBaseCenter = isShipOrFleetOnOrRightOfXPlane ? Vector3.right : Vector3.left;
            Vector3 zDirectionToDetourFromBaseCenter = isShipOrFleetOnOrFwdOfZPlane ? Vector3.forward : Vector3.back;

            // (1,0,1), (1,0,-1), (-1,0,1), (-1,0,-1)
            Vector3 directionToDetourFromBaseCenter = (xDirectionToDetourFromBaseCenter + zDirectionToDetourFromBaseCenter).normalized;
            float distanceToDetourFromBaseCenter = _distanceToClearBase + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            Vector3 detour = _baseFormationCenter + directionToDetourFromBaseCenter * distanceToDetourFromBaseCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour location directly above or below the poles of the base.
        /// The resulting detour will be located outside the operational perimeter of the base, 
        /// aka outside the formation and its close orbit slot. Which pole is determined by the position of the ship or fleet.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship or fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle
        /// at or around the belt.
        /// Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAtBasePoles(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            // Very simple: if below plane go below down pole, if on or above go above up pole
            float centerPlaneY = _baseFormationCenter.y;

            bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
            Vector3 directionToDetourFromBaseCenter = isShipOrFleetOnOrAbovePlane ? Vector3.up : Vector3.down;
            float distanceToDetourFromBaseCenter = _distanceToClearBase + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            Vector3 detour = _baseFormationCenter + directionToDetourFromBaseCenter * distanceToDetourFromBaseCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour around the poles of the base, based on where the
        /// Ship or Fleet is located. The resulting detour will be located outside the operational perimeter of the base, 
        /// aka outside the formation and its close orbit slot, but always above or below the Y value of the poles.
        /// Which pole is determined by the position of the ship or fleet.
        /// UNCLEAR This version should never encounter the same obstacle again when trying to get to the
        /// detour generated.
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAroundBasePoles(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            Vector3 directionToInitialDetourFromBaseCenter = (shipOrFleetPosition - _baseFormationCenter).normalized;
            float distanceToInitialDetourFromBaseCenter = _distanceToClearBase + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            // the initialDetour is located along the infinite line from the base center to the ship or fleet
            Vector3 initialDetour = _baseFormationCenter + directionToInitialDetourFromBaseCenter * distanceToInitialDetourFromBaseCenter;

            float centerPlaneY = _baseFormationCenter.y;
            float desiredClearanceFromPlane = _distanceToClearBase + shipOrFleetClearanceRadius;
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
                finalDetourYRelativeToPlane = Mathf.Min(finalDetourYRelativeToPlane, desiredClearanceFromPlane) + DetourDistanceBuffer;
            }
            else {        // can't be == 0 as has to be either above or below
                finalDetourYRelativeToPlane = Mathf.Max(finalDetourYRelativeToPlane, -desiredClearanceFromPlane) - DetourDistanceBuffer;
            }
            float finalDetourY = centerPlaneY + finalDetourYRelativeToPlane;
            Vector3 finalDetour = initialDetour.SetY(finalDetourY);
            return finalDetour;
        }

    }
}

