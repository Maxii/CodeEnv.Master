// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipRepairCapable.cs
// Interface for Items or stations that can repair Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items or stations that can repair Ships.
    /// <remarks>Planets, Bases and FormationStations.</remarks>
    /// </summary>
    public interface IShipRepairCapable : IRepairCapable, IShipNavigableDestination {

        /// <summary>
        /// Gets the repair capacity available in hitPts per day.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="elementOwner">The element owner.</param>
        /// <returns></returns>
        float GetAvailableRepairCapacityFor(IShip_Ltd ship, Player elementOwner);

    }
}

