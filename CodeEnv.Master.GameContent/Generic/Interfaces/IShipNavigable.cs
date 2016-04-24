﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipNavigable.cs
// INavigable destination that can be navigated to by Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// INavigable destination that can be navigated to by Ships.
    /// <remarks>All INavigable destinations are also IShipNavigable.</remarks>
    /// </summary>
    public interface IShipNavigable : INavigable {

        /// <summary>
        /// Returns the AutoPilotTarget for use by a Ship's AutoPilot when moving to this IShipNavigable destination.
        /// </summary>
        /// <param name="tgtStandoffDistance">The standoff distance from the target.</param>
        /// <param name="tgtOffset">The offset from the target that this ship is actually trying to reach.</param>
        /// <returns></returns>
        AutoPilotTarget GetMoveTarget(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition);

    }
}

