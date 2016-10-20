// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IExplorable.cs
// Base Interface for Items that can be explored by ships and fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Base Interface for Items that can be explored by ships and fleets.
    /// </summary>
    public interface IExplorable : INavigable {

        /// <summary>
        /// Occurs when the owner of this IExplorable has changed.
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

        Player Owner_Debug { get; }

        /// <summary>
        /// Indicates whether this item has been fully explored by player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsFullyExploredBy(Player player);

        /// <summary>
        /// Indicates whether the player is currently allowed to explore this item.
        /// A player is always allowed to explore items if the player doesn't know who, if anyone, is the owner.
        /// A player is not allowed to explore items if the player knows who owns the item and they are at war.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsExploringAllowedBy(Player player);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

    }
}

