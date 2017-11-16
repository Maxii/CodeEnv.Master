// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOwnerItem.cs
// Interface for easy access to Items that support having an Owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to Items that support having an Owner, including Sectors.
    /// </summary>
    public interface IOwnerItem : IDebugable {

        /// <summary>
        /// Occurs when the owner of this <c>IOwnerItem</c> is about to change.
        /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
        /// </summary>
        event EventHandler<OwnerChangingEventArgs> ownerChanging;

        /// <summary>
        /// Occurs when the owner of this <c>IOwnerItem</c> has changed.
        /// </summary>
        event EventHandler ownerChanged;

        /// <summary>
        /// The display name of this item.
        /// </summary>
        string Name { get; set; }

        Player Owner { get; }
        float Radius { get; }
        Vector3 Position { get; }

        Topography Topography { get; }

    }
}

