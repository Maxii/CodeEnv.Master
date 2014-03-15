// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFormationStationTracker.cs
// Interface for access to OnStationTracker.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for access to OnStationTracker.
    /// </summary>
    public interface IFormationStationTracker {

        event Action<Guid, bool> onShipOnStation;

        bool IsOnStation { get; }

        Guid ID { get; }

        float StationRadius { get; }

        /// <summary>
        /// The Vector3 offset of this station of the formation from the HQ Element.
        /// </summary>
        Vector3 StationOffset { get; set; }

        IMortalTarget AssignedShip { get; set; }

    }
}

