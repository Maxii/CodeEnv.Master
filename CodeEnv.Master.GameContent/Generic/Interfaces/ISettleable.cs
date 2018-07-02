// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISettleable.cs
// Interface for Items that can be settled by Fleets containing ColonyShip(s).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Items that can be settled by Fleets containing ColonyShip(s).
    /// <remarks>Currently limited to Systems.</remarks>
    /// </summary>
    [System.Obsolete("No point in having an interface that is only implemented by one item, the System.")]
    public interface ISettleable : INavigableDestination {

        /// <summary>
        /// Indicates whether the player is currently allowed to settle this item.
        /// A player is always allowed to settle items if the player doesn't know who, if anyone, is the owner.
        /// A player is not allowed to settle items if the player knows the item is owned.
        /// <remarks>6.4.18 Currently, the only items that can be settled are unowned Systems.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsFoundingSettlementAllowedBy(Player player);

        /// <summary>
        /// Returns the settlement station (StationaryLocation) that is closest to worldLocation.
        /// <remarks>Typically, worldLocation is the current position of the ColonyShip that is attempting 
        /// to create a Settlement within the System's OrbitSlot reserved for such.</remarks>
        /// </summary>
        /// <param name="worldLocation">The location in world coordinates.</param>
        /// <returns></returns>
        StationaryLocation GetClosestSettlementStationTo(Vector3 worldLocation);

    }
}

