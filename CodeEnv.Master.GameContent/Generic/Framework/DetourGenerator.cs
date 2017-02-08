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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// General purpose generator of ship detour locations used by Items that implement IAvoidableObstacle.
    /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
    /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
    /// </summary>
    public class DetourGenerator {

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The distance buffer used to make sure detours are located far enough 
        /// away from the obstacle to avoid failing the Asserts in IsDetourReachable.
        /// </summary>
        protected const float DetourDistanceBuffer = 0.1F;

        public string DebugName { get; private set; }

        private Reference<Vector3> _obstacleZoneCenterRef;
        private Vector3 _obstacleZoneCenter;
        /// <summary>
        /// The center of the obstacle zone in world space.
        /// </summary>
        protected Vector3 ObstacleZoneCenter {
            get {
                if (_obstacleZoneCenterRef != null) {
                    return _obstacleZoneCenterRef.Value;
                }
                return _obstacleZoneCenter;
            }
        }

        protected float _obstacleZoneRadius;
        private float _distanceToClearObstacle;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetourGenerator" /> class.
        /// </summary>
        /// <param name="clientDebugName">DebugName of the client.</param>
        /// <param name="obstacleZoneCenter">The center of the mobile AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        public DetourGenerator(string clientDebugName, Reference<Vector3> obstacleZoneCenter, float obstacleZoneRadius, float distanceToClearObstacle) {
            DebugName = DebugNameFormat.Inject(clientDebugName, typeof(DetourGenerator).Name);
            _obstacleZoneCenterRef = obstacleZoneCenter;
            _obstacleZoneRadius = obstacleZoneRadius;
            _distanceToClearObstacle = distanceToClearObstacle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DetourGenerator" /> class.
        /// </summary>
        /// <param name="clientDebugName">DebugName of the client.</param>
        /// <param name="obstacleZoneCenter">The center of the stationary AvoidableObstacleZone in worldspace.</param>
        /// <param name="obstacleZoneRadius">The radius of the AvoidableObstacleZone.</param>
        /// <param name="distanceToClearObstacle">The distance desired to clear the obstacle measured from AvoidableObstacleZone center.</param>
        public DetourGenerator(string clientDebugName, Vector3 obstacleZoneCenter, float obstacleZoneRadius, float distanceToClearObstacle) {
            DebugName = DebugNameFormat.Inject(clientDebugName, typeof(DetourGenerator).Name);
            _obstacleZoneCenter = obstacleZoneCenter;
            _obstacleZoneRadius = obstacleZoneRadius;
            _distanceToClearObstacle = distanceToClearObstacle;
        }

        /// <summary>
        /// Generates a detour location based on where the Obstacle's AvoidableObstacleZone was hit. The resulting detour
        /// will be located outside the zone and as a function of the zone hit point.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship or fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle. 
        /// Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The zone hit point.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourFromObstacleZoneHit(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            Vector3 ptOnZonePerimeterOnWayToDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, ObstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToDetourFromObstacleCenter = (ptOnZonePerimeterOnWayToDetour - ObstacleZoneCenter).normalized;
            float distanceToDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            Vector3 detour = ObstacleZoneCenter + directionToDetourFromObstacleCenter * distanceToDetourFromObstacleCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour location in one of the 4 XZ quadrants around the belt of the obstacle.
        /// The resulting detour will be located outside the zone. Which quadrant is determined by the position of the ship or fleet.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship of fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle
        /// at or around the poles.
        /// Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAtObstacleBelt(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            // Very simple: if below plane go below down pole, if on or above go above up pole
            float centerPlaneX = ObstacleZoneCenter.x;
            float centerPlaneZ = ObstacleZoneCenter.z;

            bool isShipOrFleetOnOrRightOfXPlane = shipOrFleetPosition.x - centerPlaneX >= Constants.ZeroF;
            bool isShipOrFleetOnOrFwdOfZPlane = shipOrFleetPosition.z - centerPlaneZ >= Constants.ZeroF;

            Vector3 xDirectionToDetourFromObstacleCenter = isShipOrFleetOnOrRightOfXPlane ? Vector3.right : Vector3.left;
            Vector3 zDirectionToDetourFromObstacleCenter = isShipOrFleetOnOrFwdOfZPlane ? Vector3.forward : Vector3.back;

            // (1,0,1), (1,0,-1), (-1,0,1), (-1,0,-1)
            Vector3 directionToDetourFromObstacleCenter = (xDirectionToDetourFromObstacleCenter + zDirectionToDetourFromObstacleCenter).normalized;
            float distanceToDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            Vector3 detour = ObstacleZoneCenter + directionToDetourFromObstacleCenter * distanceToDetourFromObstacleCenter;
            return detour;
        }


        /// <summary>
        /// Generates a detour location directly above or below the poles of the obstacle.
        /// The resulting detour will be located outside the zone. Which pole is determined by the position of the ship or fleet.
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
        public Vector3 GenerateDetourAtObstaclePoles(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            // Very simple: if below plane go below down pole, if on or above go above up pole
            float centerPlaneY = ObstacleZoneCenter.y;

            bool isShipOrFleetOnOrAbovePlane = shipOrFleetPosition.y - centerPlaneY >= Constants.ZeroF;
            Vector3 directionToDetourFromObstacleCenter = isShipOrFleetOnOrAbovePlane ? Vector3.up : Vector3.down;
            float distanceToDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            Vector3 detour = ObstacleZoneCenter + directionToDetourFromObstacleCenter * distanceToDetourFromObstacleCenter;
            return detour;
        }

        /// <summary>
        /// Generates a detour around the poles of the obstacle based on where the
        /// Ship or Fleet is located. The resulting detour will be located outside the
        /// zone as a function of the ship or fleet position, but always above or below the Y value of the poles.
        /// This version should never encounter the same obstacle again when trying to get to the
        /// detour generated.
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourAroundObstaclePoles(Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            Vector3 directionToInitialDetourFromObstacleCenter = (shipOrFleetPosition - ObstacleZoneCenter).normalized;
            float distanceToInitialDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            // the initialDetour is located along the infinite line from the obstacle to the ship or fleet
            Vector3 initialDetour = ObstacleZoneCenter + directionToInitialDetourFromObstacleCenter * distanceToInitialDetourFromObstacleCenter;

            float centerPlaneY = ObstacleZoneCenter.y;
            float desiredClearanceFromPlane = _distanceToClearObstacle + shipOrFleetClearanceRadius;
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

        /// <summary>
        /// Generates a detour location around the belt of the obstacle based on where the
        /// Obstacle's Zone was hit. The resulting detour will be located outside the
        /// zone as a function of the zone hit point, but always right or left of the belt's X value,
        /// and fwd or back of the belt's Z value.
        /// Which belt quadrant is determined by the position of the ship or fleet.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship or fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle
        /// around the poles. Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, 
        /// choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The zone hit point.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourFromZoneHitAroundBelt(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            Vector3 ptOnZonePerimeterOnWayToInitialDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, ObstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToInitialDetourFromObstacleCenter = (ptOnZonePerimeterOnWayToInitialDetour - ObstacleZoneCenter).normalized;
            float distanceToInitialDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            // the initialDetour is located along the infinite line connecting the obstacle and the point derived from the zoneHitPt
            Vector3 initialDetour = ObstacleZoneCenter + directionToInitialDetourFromObstacleCenter * distanceToInitialDetourFromObstacleCenter;

            float desiredClearanceFromPlane = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            // Calc FinalDetour X value ...
            float centerPlaneX = ObstacleZoneCenter.x;
            float initialDetourXRelativeToPlane = initialDetour.x - centerPlaneX;
            // place detour right or left of plane
            float finalDetourXRelativeToPlane = initialDetourXRelativeToPlane;
            if (Mathfx.Approx(initialDetourXRelativeToPlane, Constants.ZeroF, .01F)) {
                // initialDetour is on plane so finalDetour placement right or left of plane determined by shipOrFleetPosition
                bool isShipOrFleetOnOrRightOfPlane = shipOrFleetPosition.x - centerPlaneX >= Constants.ZeroF;
                finalDetourXRelativeToPlane += isShipOrFleetOnOrRightOfPlane ? desiredClearanceFromPlane : -desiredClearanceFromPlane;
            }
            else if (initialDetourXRelativeToPlane > Constants.ZeroF) {
                // initialDetour is right of plane
                finalDetourXRelativeToPlane += desiredClearanceFromPlane;
            }
            else {
                // initialDetour is left of plane
                finalDetourXRelativeToPlane -= desiredClearanceFromPlane;
            }
            // avoid going right or left of plane more than needed
            if (finalDetourXRelativeToPlane > Constants.ZeroF) {
                finalDetourXRelativeToPlane = Mathf.Min(finalDetourXRelativeToPlane, desiredClearanceFromPlane);
            }
            else {        // can't be == 0 as has to be either right or left
                finalDetourXRelativeToPlane = Mathf.Max(finalDetourXRelativeToPlane, -desiredClearanceFromPlane);
            }
            float finalDetourX = centerPlaneX + finalDetourXRelativeToPlane;


            // ... then FinalDetour Z value
            float centerPlaneZ = ObstacleZoneCenter.z;
            float initialDetourZRelativeToPlane = initialDetour.z - centerPlaneZ;
            // place detour fwd or back of plane
            float finalDetourZRelativeToPlane = initialDetourZRelativeToPlane;
            if (Mathfx.Approx(initialDetourZRelativeToPlane, Constants.ZeroF, .01F)) {
                // initialDetour is on plane so finalDetour placement fwd or back of plane determined by shipOrFleetPosition
                bool isShipOrFleetOnOrFwdOfPlane = shipOrFleetPosition.z - centerPlaneZ >= Constants.ZeroF;
                finalDetourZRelativeToPlane += isShipOrFleetOnOrFwdOfPlane ? desiredClearanceFromPlane : -desiredClearanceFromPlane;
            }
            else if (initialDetourZRelativeToPlane > Constants.ZeroF) {
                // initialDetour is fwd of plane
                finalDetourZRelativeToPlane += desiredClearanceFromPlane;
            }
            else {
                // initialDetour is back of plane
                finalDetourZRelativeToPlane -= desiredClearanceFromPlane;
            }

            // avoid going fwd or back of plane more than needed
            if (finalDetourZRelativeToPlane > Constants.ZeroF) {
                finalDetourZRelativeToPlane = Mathf.Min(finalDetourZRelativeToPlane, desiredClearanceFromPlane);
            }
            else {        // can't be == 0 as has to be either fwd or back
                finalDetourZRelativeToPlane = Mathf.Max(finalDetourZRelativeToPlane, -desiredClearanceFromPlane);
            }
            float finalDetourZ = centerPlaneZ + finalDetourZRelativeToPlane;

            Vector3 finalDetour = initialDetour.SetX(finalDetourX);
            finalDetour = finalDetour.SetZ(finalDetourZ);
            return finalDetour;
        }


        /// <summary>
        /// Generates a detour location around the poles of the obstacle based on where the
        /// Obstacle's Zone was hit. The resulting detour will be located outside the
        /// zone as a function of the zone hit point, but always above or below the pole's Y value.
        /// Which pole is determined by the position of the ship or fleet.
        /// <remarks>This algorithm can result in detours that cannot be reached by the ship or fleet without 
        /// encountering the same obstacle. This most commonly happens when they are too close to the obstacle
        /// around the belt. Use IsDetourCleanlyReachable() to determine whether this is the case, and if so, 
        /// choose another algorithm.</remarks>
        /// <remarks>The detour locations do not account for reqd offsets when ships are traveling as fleets.
        /// This is handled later when the Detour's ApDestinationProxy is generated for each ship.</remarks>
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The zone hit point.</param>
        /// <param name="shipOrFleetClearanceRadius">The clearance radius reqd by the ship or fleet.</param>
        /// <returns></returns>
        public Vector3 GenerateDetourFromZoneHitAroundPoles(Vector3 shipOrFleetPosition, Vector3 zoneHitPt, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _obstacleZoneRadius, shipOrFleetPosition), DebugName);
            Vector3 ptOnZonePerimeterOnWayToInitialDetour = MyMath.FindClosestPointOnSphereOrthogonalToIntersectingLine(shipOrFleetPosition, zoneHitPt, ObstacleZoneCenter, _obstacleZoneRadius);
            Vector3 directionToInitialDetourFromObstacleCenter = (ptOnZonePerimeterOnWayToInitialDetour - ObstacleZoneCenter).normalized;
            float distanceToInitialDetourFromObstacleCenter = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
            // the initialDetour is located along the infinite line connecting the obstacle and the point derived from the zoneHitPt
            Vector3 initialDetour = ObstacleZoneCenter + directionToInitialDetourFromObstacleCenter * distanceToInitialDetourFromObstacleCenter;

            float centerPlaneY = ObstacleZoneCenter.y;
            float desiredClearanceFromPlane = _distanceToClearObstacle + shipOrFleetClearanceRadius + DetourDistanceBuffer;
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

        /// <summary>
        /// Returns <c>true</c> if the detour is reachable by the ship or fleet without any contact between the ship(s) collision
        /// detection zone(s) and the Obstacle's Zone, <c>false</c> if it definitely isn't or might not be.
        /// <remarks>If this returns <c>true</c>, the client can expect that the ship or fleet's clearance zone will not
        /// contact the obstacle's zone. Even if it does, ships will auto correct to separate from the obstacle.</remarks>
        /// <remarks>This calculation is very conservative using line points from the perimeter of the 
        /// shipOrFleet's clearanceZone closest to the obstacle.</remarks>
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The ship or fleet clearance radius.</param>
        /// <returns></returns>
        public bool IsDetourCleanlyReachable(Vector3 detour, Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _distanceToClearObstacle, detour), DebugName);
            //D.Log("{0}.IsDetourCleanlyReachable() called.", DebugName);

            // Calc pt in between shipOrFleet and obstacle that tries to be just outside shipOrFleetClearanceRadius away from shipOrFleet without being inside the obstacleZone
            Vector3 directionToObstacleFromShipOrFleet = (ObstacleZoneCenter - shipOrFleetPosition).normalized;

            float distanceFromShipOrFleetToObstacleCenter = Vector3.Distance(shipOrFleetPosition, ObstacleZoneCenter);
            float distanceFromShipOrFleetToPtThatClearsObstacle = distanceFromShipOrFleetToObstacleCenter - _distanceToClearObstacle;
            if (distanceFromShipOrFleetToPtThatClearsObstacle <= Constants.ZeroF) {
                // shipOrFleet is already inside _distanceToClearObstacle so we know it will intersect the sphere
                return false;
            }

            float distanceFromShipOrFleetToClosestPtToObstacleOutsideClearanceZone = Mathf.Min(distanceFromShipOrFleetToPtThatClearsObstacle, shipOrFleetClearanceRadius);
            Vector3 shipOrFleetClosestPtToObstacleOutsideClearanceZone = shipOrFleetPosition + directionToObstacleFromShipOrFleet * distanceFromShipOrFleetToClosestPtToObstacleOutsideClearanceZone;

            // Calc pt in between detour and obstacle that tries to be just outside shipOrFleetClearanceRadius away from detour
            Vector3 directionToObstacleFromDetour = (ObstacleZoneCenter - detour).normalized;
            Vector3 detourClosestPtToObstacleOutsideClearanceZone = detour + (directionToObstacleFromDetour * shipOrFleetClearanceRadius);
            // detour should always be at least shipOrFleetClearanceRadius + _distanceToClearObstacle away from obstacle
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _distanceToClearObstacle, detourClosestPtToObstacleOutsideClearanceZone), DebugName);

            Vector3 linePt1 = shipOrFleetClosestPtToObstacleOutsideClearanceZone;
            Vector3 linePt2 = detourClosestPtToObstacleOutsideClearanceZone;

            // If linePt starts inside of sphere, the line will always intersect the sphere. 
            // The other end of the line will never be inside the sphere
            return !MyMath.DoesLineSegmentIntersectSphere(linePt1, linePt2, ObstacleZoneCenter, _distanceToClearObstacle);
        }

        /// <summary>
        /// Returns <c>true</c> if the detour is reachable by the ship or fleet, aka a ray cast from the ship or fleet
        /// to the detour will not encounter the obstacle zone, <c>false</c> if it is not. 
        /// <remarks>Warning: If the obstacle is a planet its _distanceToClearObstacle will be larger than the radius 
        /// of the obstacle zone because of the potential presence of close orbiting ships. Even if one or more of 
        /// orbiting ships are encountered, the ship will still auto correct to separate from the orbiting ship(s).</remarks>
        /// <remarks>Warning: The ship or fleet's clearance zone can potentially contact the obstacle's zone.
        /// If it does, ships will still auto correct to separate from the obstacle.</remarks>
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="shipOrFleetClearanceRadius">The ship or fleet clearance radius.</param>
        /// <returns></returns>
        public bool IsDetourReachable(Vector3 detour, Vector3 shipOrFleetPosition, float shipOrFleetClearanceRadius) {
            D.Assert(!MyMath.IsPointOnOrInsideSphere(ObstacleZoneCenter, _distanceToClearObstacle, detour), DebugName);
            //D.Log("{0}.IsDetourReachable() called.", DebugName);

            Vector3 linePt1 = shipOrFleetPosition;
            Vector3 linePt2 = detour;

            // Warning: Use of _distanceToClearObstacle rather than _obstacleZoneRadius can't work here as a ship can be
            // in close orbit which means its position will start inside a closeOrbitSlot, typically included 
            // in _distanceToClearObstacle on obstacles that allow close orbiting.
            return !MyMath.DoesLineSegmentIntersectSphere(linePt1, linePt2, ObstacleZoneCenter, _obstacleZoneRadius);
        }

        /// <summary>
        /// Gets the approach path derived from the direction of approach.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitPt">The obstacle zone hit point.</param>
        /// <returns></returns>
        public ApproachPath GetApproachPath(Vector3 shipOrFleetPosition, Vector3 zoneHitPt) {
            float topPoleY = ObstacleZoneCenter.y + _obstacleZoneRadius;
            float bottomPoleY = ObstacleZoneCenter.y - _obstacleZoneRadius;
            bool isShipOrFleetAboveOrBelowPoles = shipOrFleetPosition.y > topPoleY || shipOrFleetPosition.y < bottomPoleY;

            Vector3 approachDirection = (zoneHitPt - shipOrFleetPosition).normalized;
            float yCorrelation = Mathf.Abs(Vector3.Dot(approachDirection, Vector3.up));
            float xCorrelation = Mathf.Abs(Vector3.Dot(approachDirection, Vector3.right));
            float zCorrelation = Mathf.Abs(Vector3.Dot(approachDirection, Vector3.forward));

            if (isShipOrFleetAboveOrBelowPoles && (yCorrelation > xCorrelation && yCorrelation > zCorrelation)) {
                return ApproachPath.Polar;
            }
            return ApproachPath.Belt;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        /// <summary>
        /// Enum indicating the predominant approach path to the
        /// Obstacle that owns this DetourGenerator.
        /// </summary>
        public enum ApproachPath {

            None,

            Polar,
            Belt
        }


        #endregion

    }
}

