// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalItem.cs
// Interface for a MortalItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a MortalItem.
    /// </summary>
    public interface IMortalItem : IDestinationItem {

        event Action<IMortalItem> onItemDeath;

        event Action<IMortalItem> onOwnerChanged;

        bool IsDead { get; }

        void TakeDamage(float damage);

        IPlayer Owner { get; }

        string ParentName { get; }

    }
}

