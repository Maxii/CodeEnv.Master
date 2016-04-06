﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAvoidableObstacle.cs
// Interface for IObstacle Items that can be avoided before ship passage is impeded.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for IObstacle Items that can be avoided before ship passage is impeded.
    /// </summary>
    public interface IAvoidableObstacle : IObstacle {

        /// <summary>
        /// Returns the detour to get by this avoidable obstacle. Detours do not
        /// account for a ship's formation station offset. Ship navigation handles that itself.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitInfo">The zone hit information.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <returns></returns>
        Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius);

    }
}

