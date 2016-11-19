// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuardable.cs
// Interface for Items that can be guarded by Fleets at GuardStations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that can be guarded by Fleets at GuardStations.
    /// Includes Systems, Sectors, Bases and the UniverseCenter.    // IDEA: Coincident with IPatrollable
    /// </summary>
    public interface IGuardable : INavigable {  // IDEA: Could : IFleetNavigable but why?

        /// <summary>
        /// Occurs when the owner of this IGuardable has changed.
        /// <remarks>OK for client to have access to this, even if they don't have access
        /// to Owner info as long as they use the event to properly check for Owner access.</remarks>
        /// </summary>
        event EventHandler ownerChanged;

        /// <summary>
        /// Occurs when InfoAccess rights change for a player on an item, directly attributable to
        /// a change in the player's IntelCoverage of the item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

        IList<StationaryLocation> GuardStations { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

        Player Owner_Debug { get; }

        /// <summary>
        /// Indicates whether the player is currently allowed to guard this item.
        /// A player is always allowed to guard items if the player doesn't know who, if anyone, is the owner.
        /// A player is not allowed to guard items if the player knows who owns the item and they are enemies.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///   <c>true</c> if [is guarding allowed by] [the specified player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsGuardingAllowedBy(Player player);

    }
}

