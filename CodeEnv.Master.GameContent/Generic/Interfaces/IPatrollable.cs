// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPatrollable.cs
// Interface for Items that are patrollable by Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that are patrollable by Fleets.
    /// Includes Systems, Sectors, Bases and the UniverseCenter.    // IDEA: Coincident with IGuardable
    /// </summary>
    public interface IPatrollable : INavigable {    // IDEA: Could : IFleetNavigable but why?

        /// <summary>
        /// Occurs when the owner of this IPatrollable has changed.
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

        /// <summary>
        /// Returns a copy of the list of Patrol Stations around this IPatrollable Item.
        /// <remarks>A copy allows the list to be modified without affecting the original list.</remarks>
        /// </summary>
        IList<StationaryLocation> PatrolStations { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

        Player Owner_Debug { get; }

        Speed PatrolSpeed { get; }

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

        /// <summary>
        /// Indicates whether the player is currently allowed to patrol around this item.
        /// A player is always allowed to patrol items if the player doesn't know who, if anyone, is the owner.
        /// A player is not allowed to patrol items if the player knows who owns the item and they are enemies.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///   <c>true</c> if [is patrolling allowed by] [the specified player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsPatrollingAllowedBy(Player player);

    }
}

