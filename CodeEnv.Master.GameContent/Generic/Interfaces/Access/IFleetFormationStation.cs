// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetFormationStation.cs
// Interface for easy access to FleetFormationStation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to FleetFormationStation.
    /// </summary>
    public interface IFleetFormationStation {

        bool IsOnStation { get; }

        Vector3 LocalOffset { get; }

        float __DistanceToOnStation { get; }

        /// <summary>
        /// Returns <c>true</c> if AssignedShip is still making progress toward this station, <c>false</c>
        /// if progress is no longer being made as the ship has arrived OnStation. If still making
        /// progress, direction and distance to the station are valid.
        /// </summary>
        /// <param name="onStationDirection">The direction to being OnStation.</param>
        /// <param name="onStationDistance">The distance to being OnStation.</param>
        /// <returns></returns>
        bool TryCheckProgressTowardStation(out Vector3 onStationDirection, out float onStationDistance);


    }
}

