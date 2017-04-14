// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAttackable.cs
// Interface for Items that can be attacked, aka MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Items that can be attacked, aka MortalItems.
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
        /// Indicates whether the attackingPlayer is allowed to attack this item.
        /// A player is only allowed to attack this item if the player knows who the owner is, and 
        /// 1) a state of War exists between the two, or 
        /// 2) a state of ColdWar exists between the two and the item is located within the player's territory.
        /// </summary>
        /// <param name="attackingPlayer">The player who wishes to attack.</param>
        /// <returns>
        ///   <c>true</c> if [is attack allowed] [the specified attacking player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsAttackAllowedBy(Player attackingPlayer);

        /// <summary>
        /// Indicates whether the attackingPlayer is allowed to attack this item.
        /// 4.12.17 An 'attackingPlayer' is only allowed to attack this item if the attackingPlayer knows who the owner is, 
        /// a state of ColdWar exists between the two and the item is located within the attackingPlayer's territory.
        /// </summary>
        /// <param name="attackingPlayer">The player who wishes to attack.</param>
        /// <returns>
        ///   <c>true</c> if [is cold war attack allowed] [the specified attacking player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsColdWarAttackAllowedBy(Player attackingPlayer);

        /// <summary>
        /// Indicates whether the player is allowed to attack this item.
        /// A player is only allowed to attack this item if the player knows who the owner is and a state of War exists between the two.
        /// </summary>
        /// <param name="attackingPlayer">The player who wishes to attack.</param>
        /// <returns>
        ///   <c>true</c> if [is war attack allowed] [the specified attacking player]; otherwise, <c>false</c>.
        /// </returns>
        bool IsWarAttackAllowedBy(Player attackingPlayer);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);


    }
}

