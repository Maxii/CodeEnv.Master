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
        /// Occurs when InfoAccess rights change for a player on an item.
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

        bool IsPatrollingAllowedBy(Player player);

    }
}

