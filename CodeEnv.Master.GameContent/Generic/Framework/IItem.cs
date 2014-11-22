// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IItem.cs
//  Interface for all items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    ///  Interface for all items.
    /// </summary>
    public interface IItem {

        /// <summary>
        /// The name of this individual Item.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The name to use for display in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The name to use for debugging. Includes parent name.
        /// </summary>
        string FullName { get; }

        float Radius { get; }

        event Action<IItem> onOwnerChanged;
        IPlayer Owner { get; }

        Vector3 Position { get; }
        Transform Transform { get; }

    }
}

