// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFormationStation.cs
// Interface for access to FormationStation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for access to FormationStation.
    /// </summary>
    public interface IFormationStation {

        bool IsOnStation { get; }

        float StationRadius { get; }

        Vector3 Position { get; }

        /// <summary>
        /// The offset of this formation station from the HQ Element.
        /// </summary>
        Vector3 StationOffset { get; set; }

        IShipModel AssignedShip { get; set; }

        /// <summary>
        /// The vector from the currently assigned ship to the station.
        /// </summary>
        Vector3 VectorToStation { get; }

    }
}

