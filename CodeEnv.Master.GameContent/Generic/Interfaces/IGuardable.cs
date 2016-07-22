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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that can be guarded by Fleets at GuardStations.
    /// Includes Systems, Sectors, Bases and the UniverseCenter.    // IDEA: Coincident with IPatrollable
    /// </summary>
    public interface IGuardable : INavigable {  // IDEA: Could : IFleetNavigable but why?

        /// <summary>
        /// Occurs when InfoAccess rights change for a player on an item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChanged;

        IList<StationaryLocation> GuardStations { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

        Player Owner_Debug { get; }

        bool IsGuardingAllowedBy(Player player);

    }
}

