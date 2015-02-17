// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICommandItem.cs
// Interface for items that are unit commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for items that are unit commands.
    /// </summary>
    public interface ICommandItem : IMortalItem {

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> is about to change.
        /// The new incoming owner is the <c>Player</c> provided.
        /// </summary>
        event Action<IItem, Player> onOwnerChanging;

        /// <summary>
        /// Occurs when the owner of this <c>IItem</c> has changed.
        /// </summary>
        event Action<IItem> onOwnerChanged;

    }
}

