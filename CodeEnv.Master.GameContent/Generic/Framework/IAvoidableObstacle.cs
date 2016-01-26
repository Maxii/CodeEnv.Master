// --------------------------------------------------------------------------------------------------------------------
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
        /// Returns the detour to get by this avoidable obstacle. Detours always
        /// account for the ship's offset in the formation. If a fleet or the Flagship, 
        /// the offset will be Vector3.zero.
        /// </summary>
        /// <param name="shipOrFleetPosition">The ship or fleet position.</param>
        /// <param name="zoneHitInfo">The zone hit information.</param>
        /// <param name="fleetRadius">The fleet radius.</param>
        /// <param name="formationOffset">The formation offset.</param>
        /// <returns></returns>
        Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius, Vector3 formationOffset);

    }
}

