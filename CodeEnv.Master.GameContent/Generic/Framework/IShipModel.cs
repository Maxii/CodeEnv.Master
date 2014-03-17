﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipModel.cs
// Interface for ShipModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for ShipModels.
    /// </summary>
    public interface IShipModel : IElementModel {

        new ShipData Data { get; set; }

        UnitOrder<ShipOrders> CurrentOrder { get; set; }

        IFleetCmdModel Command { get; set; }

        /// <summary>
        /// Changes the speed of the ship.
        /// </summary>
        /// <param name="speed">The new speed request.</param>
        /// <param name="isAutoPilot">if set to <c>true</c>the requester is the autopilot.</param>
        void ChangeSpeed(Speed speed, bool isAutoPilot);

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="isAutoPilot">if set to <c>true</c> the requester is the autopilot.</param>
        void ChangeHeading(Vector3 newHeading, bool isAutoPilot);

    }
}

