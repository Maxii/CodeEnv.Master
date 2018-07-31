// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipRepairCapable.cs
// Interface for Items or stations that can repair Ships and CmdModules.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items or stations that can repair Ships and FleetCmdModules.
    /// <remarks>Planets, Bases and FormationStations.</remarks>
    /// </summary>
    public interface IShipRepairCapable : IRepairCapable, IShipNavigableDestination {

        /// <summary>
        /// Gets the repair capacity available to repair the ship in hitPts per day.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="elementOwner">The element owner.</param>
        /// <returns></returns>
        float GetAvailableRepairCapacityFor(IShip_Ltd ship, Player elementOwner);

        /// <summary>
        /// Gets the repair capacity available to repair the Fleet's CmdModule in hitPts per day.
        /// </summary>
        /// <param name="unusedFleetCmd">The unused fleet command.</param>
        /// <param name="flagship">The flagship.</param>
        /// <param name="cmdOwner">The command owner.</param>
        /// <returns></returns>
        float GetAvailableRepairCapacityFor(IFleetCmd_Ltd unusedFleetCmd, IShip_Ltd flagship, Player cmdOwner);


    }
}

