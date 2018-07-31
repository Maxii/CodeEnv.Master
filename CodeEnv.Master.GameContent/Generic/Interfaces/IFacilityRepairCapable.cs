// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFacilityRepairCapable.cs
// Interface for Items or stations that can repair Facilities and CmdModules.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items or stations that can repair Facilities and BaseCmdModules.
    /// <remarks>Bases.</remarks>
    /// <remarks>Anything that can repair a facility can repair a ship.</remarks>
    /// </summary>
    public interface IFacilityRepairCapable : IShipRepairCapable {

        /// <summary>
        /// Gets the repair capacity available in hitPts per day.
        /// </summary>
        /// <param name="facility">The facility.</param>
        /// <param name="elementOwner">The element owner.</param>
        /// <returns></returns>
        float GetAvailableRepairCapacityFor(IFacility_Ltd facility, Player elementOwner);

        /// <summary>
        /// Gets the repair capacity available to repair the Base's CmdModule in hitPts per day.
        /// </summary>
        /// <param name="unusedBaseCmd">The unused Base command.</param>
        /// <param name="cmdOwner">The command owner.</param>
        /// <returns></returns>
        float GetAvailableRepairCapacityFor(IUnitBaseCmd_Ltd unusedBaseCmd, Player cmdOwner);


    }
}

