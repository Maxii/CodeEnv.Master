// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipCloseOrbitSimulator.cs
// Interface for easy access to ShipCloseOrbitSimulator instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to ShipCloseOrbitSimulator instances.
    /// </summary>
    public interface IShipCloseOrbitSimulator : IOrbitSimulator {

        /// <summary>
        /// Returns true if the provided ship should be manually placed in the returned
        /// position within the close orbit slot, false if the ship should use the autoPilot
        /// to achieve close orbit.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="closeOrbitPlacementPosition">The close orbit placement position.</param>
        /// <returns></returns>
        bool TryDetermineCloseOrbitPlacementPosition(IShipItem ship, out Vector3 closeOrbitPlacementPosition);

    }
}

