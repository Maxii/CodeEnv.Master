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
        /// Occurs when InfoAccess rights change for a player on an item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChanged;

        Player Owner_Debug { get; }

        /// <summary>
        /// Indicates whether this item has been fully explored by player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsFullyExploredBy(Player player);

        /// <summary>
        /// Indicates whether the player is currently allowed to explore this item.
        /// In general, a player are not allowed to explore items owned by a player
        /// with whom they are at war.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        bool IsExploringAllowedBy(Player player);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

    }
}

