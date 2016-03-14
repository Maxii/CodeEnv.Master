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

    /// <summary>
    /// Base Interface for Items that can be explored by ships and fleets.
    /// </summary>
    public interface IExplorable : INavigableTarget {

        Player Owner { get; }

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


    }
}

