// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetNavigableDestination.cs
// INavigableDestination that can be navigated to by Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// INavigableDestination that can be navigated to by Fleets.
    /// <remarks>Elements, FormationStations and CloseOrbitSimulators are not IFleetNavigableDestinations.
    /// Stationary and MobileLocations are IFleetNavigableDestinations as GuardStation, PatrolStation and 
    /// AssemblyStation destinations are all StationaryLocations.</remarks>
    /// <remarks>Used only by FleetNavigator.</remarks>
    /// </summary>
    public interface IFleetNavigableDestination : INavigableDestination {

        Topography Topography { get; }

        float GetObstacleCheckRayLength(Vector3 fleetPosition);

    }
}

