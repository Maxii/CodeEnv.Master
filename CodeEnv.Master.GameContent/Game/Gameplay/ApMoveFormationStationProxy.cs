// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApMoveFormationStationProxy.cs
// Proxy used by a Ship's AutoPilot to navigate to its FleetFormationStation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Proxy used by a Ship's AutoPilot to navigate to its FleetFormationStation.
    /// <remarks>Overrides ApMoveDestinationProxy in order to use FormationStation's methods
    /// so that consistent results are achieved.</remarks>
    /// </summary>
    public class ApMoveFormationStationProxy : ApMoveDestinationProxy {

        public new IFleetFormationStation Destination { get { return base.Destination as IFleetFormationStation; } }

        public override bool HasArrived { get { return Destination.IsOnStation; } }

        public override float __ShipDistanceFromArrived { get { return Destination.__DistanceToOnStation; } }

        public ApMoveFormationStationProxy(IFleetFormationStation station, IShip ship, float innerRadius, float outerRadius)
            : base(station as IShipNavigableDestination, ship, innerRadius, outerRadius) {
        }

        public override bool TryCheckProgress(out Vector3 direction, out float distance) {
            return Destination.TryCheckProgressTowardStation(out direction, out distance);
        }

    }
}

