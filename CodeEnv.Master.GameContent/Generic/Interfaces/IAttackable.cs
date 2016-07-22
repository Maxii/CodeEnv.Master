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
        /// Occurs when InfoAccess rights change for a player on an item.
        /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
        /// </summary>
        event EventHandler<InfoAccessChangedEventArgs> infoAccessChanged;

        bool IsAttackingAllowedBy(Player player);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);


    }
}

