// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOwnerItem_Ltd.cs
// Limited access Interface for easy access to Items that support having an Owner, including Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Limited access Interface for easy access to Items that support having an Owner, including Sectors.
    /// </summary>
    public interface IOwnerItem_Ltd : IDebugable {

        /// <summary>
        /// Occurs when the owner of this item has changed.
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

        bool IsOperational { get; }

        Vector3 Position { get; }

        Player Owner_Debug { get; }

        /// <summary>
        /// Debug version of TryGetOwner without the validation that 
        /// requestingPlayer already knows owner when OwnerInfoAccess is available.
        /// <remarks>Used by PlayerAIMgr's discover new players process.</remarks>
        /// </summary>
        /// <param name="requestingPlayer">The requesting player.</param>
        /// <param name="owner">The owner.</param>
        /// <returns></returns>
        bool TryGetOwner_Debug(Player requestingPlayer, out Player owner);

        /// <summary>
        /// Returns <c>true</c> if the requestingPlayer has InfoAccess to the
        /// owner of this item, <c>false</c> otherwise.
        /// <remarks>Validates that requestingPlayer knows owner if access is granted.</remarks>
        /// </summary>
        /// <param name="requestingPlayer">The requesting player.</param>
        /// <param name="owner">The owner.</param>
        /// <returns></returns>
        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

        /// <summary>
        /// Logs the current subscribers of this Item's infoAccessChgd event.
        /// <remarks>4.17.17 Used for debugging the receipt of infoAccessChgd events in unexpected places.</remarks>
        /// </summary>
        void __LogInfoAccessChangedSubscribers();


    }
}

