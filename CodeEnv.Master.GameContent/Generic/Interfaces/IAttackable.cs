// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAttackable.cs
// Interface for targets that can be attacked, aka MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for targets that can be attacked, aka MortalItems.
    /// </summary>
    public interface IAttackable {

        event EventHandler deathOneShot;

        /// <summary>
        /// Occurs when InfoAccess rights change for a player on an item, directly attributable to
        /// a change in the player's IntelCoverage of the item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

        /// <summary>
        /// Indicates whether the player is currently allowed to attack this item.
        /// A player is only allowed to attack items if the player knows who the owner is and that owner is an enemy.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///   <c>true</c> if [is attacking allowed by] [the specified player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsAttackingAllowedBy(Player player);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);


    }
}

