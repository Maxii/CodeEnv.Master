// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IItem.cs
// Interface for easy access to all items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to all items.
    /// </summary>
    public interface IItem {

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> is about to change.
        /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
        /// </summary>
        event EventHandler<OwnerChangingEventArgs> ownerChanging;

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> has changed.
        /// </summary>
        event EventHandler ownerChanged;

        Player Owner { get; }
        bool IsOperational { get; }
        float Radius { get; }
        Vector3 Position { get; }

        /// <summary>
        /// The name to use for debugging. Includes parent name.
        /// </summary>
        string FullName { get; }
        string DisplayName { get; }

    }
}

