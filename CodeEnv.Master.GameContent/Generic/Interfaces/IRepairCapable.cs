// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IRepairCapable.cs
// Interface for Items that can repair elements and cmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Items that can repair elements and cmds.
    /// <remarks>4.3.17 Currently Bases, Planets (not Moons) and FormationStations.</remarks>
    /// <remarks>Yes, all IRepairCapable destinations are IShipNavigableDestination, but why clutter it up?</remarks>
    /// </summary>
    public interface IRepairCapable : IElementNavigableDestination {

        /// <summary>
        /// Indicates whether the player is currently allowed to repair at this item.
        /// A player is always allowed to repair items if the player doesn't know who, if anyone, is the owner.
        /// A player is not allowed to repair at the item if the player knows who owns the item and they are enemies.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsRepairingAllowedBy(Player player);


    }
}

