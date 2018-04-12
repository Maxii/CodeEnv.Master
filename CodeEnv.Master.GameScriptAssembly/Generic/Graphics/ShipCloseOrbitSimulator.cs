// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCloseOrbitSimulator.cs
// Simulates ship(s) orbiting around an immobile parent.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Simulates ship(s) orbiting around an immobile parent. Ship(s) will be attached by a fixed joint.
/// This is also an INavigableDestination which allows it to be used as a destination by a Ship's AutoPilot.
/// </summary>
public class ShipCloseOrbitSimulator : OrbitSimulator, IShipCloseOrbitSimulator, IShipNavigableDestination {

    protected override float RelativeOrbitRate { get { return TempGameValues.RelativeOrbitRateOfShips; } }

    public override string ToString() {
        return DebugName;
    }

    #region IShipOrbitSimulator Members Archive

    /// <summary>
    /// Checks the ship's position to see whether the ship should simply be manually placed in close orbit.
    /// Returns <c>true</c> if the ship
    /// if located inside the orbit slot capture window, <c>false</c> otherwise.
    /// <remarks>If the ship is already properly positioned within the orbit slot capture window or it is inside
    /// the capture window(probably encountering an IObstacle zone except around bases), the AutoPilot cannot be used.
    /// In this case it is best to simply place the ship where it belongs as this should not happen often.</remarks>
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    //[System.Obsolete]
    //private bool CheckWetherToManuallyPlaceShipInCloseOrbit(IShip ship) {
    //    float shipDistanceToOrbitedObject = Vector3.Distance(ship.Position, transform.position);    // same as OrbitedItem
    //    float minOutsideOfOrbitCaptureRadius = OrbitData.OuterRadius - ship.CollisionDetectionZoneRadius;
    //    return shipDistanceToOrbitedObject < minOutsideOfOrbitCaptureRadius;
    //}

    ///// <summary>
    ///// Returns true if the provided ship should simply be placed in the returned position within
    ///// the close orbit slot, false if the ship should use the autoPilot to achieve close orbit.
    ///// </summary>
    ///// <param name="ship">The ship.</param>
    ///// <param name="closeOrbitPlacementPosition">The close orbit placement position.</param>
    ///// <returns></returns>
    //[System.Obsolete]
    //public bool TryDetermineCloseOrbitPlacementPosition(IShip_Ltd ship, out Vector3 closeOrbitPlacementPosition) {
    //    if (CheckWeatherToManuallyPlaceShipInCloseOrbit(ship)) {
    //        // ship is too far inside of orbitSlot to use AutoPilot so just place it where it belongs
    //        float slotMeanRadius = OrbitData.MeanRadius;
    //        float distanceFromOrbitedObjectToDesiredPosition = slotMeanRadius;
    //        float maxAllowedShipOrbitRadius = OrbitData.OuterRadius - ship.CollisionDetectionZoneRadius;
    //        D.Warn(distanceFromOrbitedObjectToDesiredPosition > maxAllowedShipOrbitRadius, "{0} CollisionDetectionZone is protruding from ShipOrbitSlot. {1:0.##} > {2:0.##}.",
    //            ship.DebugName, distanceFromOrbitedObjectToDesiredPosition, maxAllowedShipOrbitRadius);
    //        float minAllowedShipOrbitRadius = OrbitData.InnerRadius + ship.CollisionDetectionZoneRadius;
    //        D.Warn(distanceFromOrbitedObjectToDesiredPosition < minAllowedShipOrbitRadius, "{0} CollisionDetectionZone is protruding from ShipOrbitSlot. {1:0.##} < {2:0.##}.",
    //            ship.DebugName, distanceFromOrbitedObjectToDesiredPosition, minAllowedShipOrbitRadius);

    //        Vector3 orbitedObjectPosition = transform.position; // same as orbitedItem
    //        Vector3 directionToDesiredOrbitPosition = (ship.Position - orbitedObjectPosition).normalized;
    //        closeOrbitPlacementPosition = orbitedObjectPosition + directionToDesiredOrbitPosition * distanceFromOrbitedObjectToDesiredPosition;
    //        return true;
    //    }
    //    // Ship is outside orbit slot capture window so AutoPilot can be used
    //    closeOrbitPlacementPosition = Vector3.zero;
    //    return false;
    //}

    #endregion

    #region INavigableDestination Members

    public string Name { get { return DebugName; } }

    public override string DebugName {
        get {
            IShipCloseOrbitable closeOrbitableItem = OrbitData.OrbitedItem.GetComponent<IShipCloseOrbitable>();
            return DebugNameFormat.Inject(closeOrbitableItem.DebugName, GetType().Name);
        }
    }

    public Vector3 Position { get { return transform.position; } }

    public bool IsMobile { get { return OrbitData.IsOrbitedItemMobile; } }

    #endregion

    #region IShipNavigableDestination Members

    public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        // makes sure the entire shipCollisionDetectionZone is inside the OrbitSlot
        float outerShellRadius = OrbitData.OuterRadius - tgtStandoffDistance;
        float innerShellRadius = OrbitData.InnerRadius + tgtStandoffDistance;
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion
}

