// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IItem.cs
// Interface for easy access to MonoBehaviours that are AItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are AItems.
    /// </summary>
    public interface IItem : IDebugable {

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> is about to change.
        /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
        /// </summary>
        event EventHandler<OwnerChangingEventArgs> ownerChanging;

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> has changed.
        /// </summary>
        event EventHandler ownerChanged;

        Player Owner { get; }   // TODO will need ability to set
        bool IsOperational { get; }
        float Radius { get; }
        Vector3 Position { get; }

        string DisplayName { get; }
        string Name { get; }

        Topography Topography { get; }

        bool ShowDebugLog { get; }

    }
}

