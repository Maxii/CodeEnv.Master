// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetNavigable.cs
// INavigable destination that can be navigated to by Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// INavigable destination that can be navigated to by Fleets.
    /// <remarks>Elements, FormationStations and CloseOrbitSimulators are not IFleetNavigable.
    /// Stationary and MobileLocations are IFleetNavigable as GuardStation, PatrolStation and 
    /// AssemblyStation destinations are all StationaryLocations.</remarks>
    /// <remarks>Used only by FleetNavigator.</remarks>
    /// </summary>
    public interface IFleetNavigable : INavigable {

        Topography Topography { get; }

        float GetObstacleCheckRayLength(Vector3 fleetPosition);

    }
}

