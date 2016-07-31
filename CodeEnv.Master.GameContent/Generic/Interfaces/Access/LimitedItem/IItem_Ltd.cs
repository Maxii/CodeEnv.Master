// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IItem_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AItems.
    /// </summary>
    public interface IItem_Ltd : IDebugable {

        /// <summary>
        /// Occurs when InfoAccess rights change for a player on an item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

        bool IsOperational { get; }

        Vector3 Position { get; }

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);
    }
}

