// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IManeuverable.cs
// Interface for Items that can maneuver, aka have engines.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for Items that can maneuver, aka have engines.
    /// </summary>
    public interface IManeuverable : IDetectable, INavigableDestination /*IDetectable*/ {

        event EventHandler deathOneShot;

        /// <summary>
        /// Occurs when the owner of this <c>AItem</c> is about to change.
        /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
        /// </summary>
        event EventHandler<OwnerChangingEventArgs> ownerChanging;

        event EventHandler ownerChanged;

        new string DebugName { get; }

        new bool IsOperational { get; }

        bool IsFtlCapable { get; }

        new Vector3 Position { get; }

        void HandleFtlDampenedBy(IUnitCmd_Ltd source, RangeCategory rangeCat);

        void HandleFtlUndampenedBy(IUnitCmd_Ltd source, RangeCategory rangeCat);

        bool IsFtlDampenedBy(IUnitCmd_Ltd source);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

    }
}

