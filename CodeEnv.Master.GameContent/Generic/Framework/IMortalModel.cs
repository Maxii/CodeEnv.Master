// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalModel.cs
// Interface for a MortalItemModel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a MortalItemModel.
    /// </summary>
    public interface IMortalModel : IModel {

        event Action<IMortalModel> onItemDeath;

        event Action<IMortalModel> onOwnerChanged;

        bool IsDead { get; }

        void TakeDamage(float damage);

        IPlayer Owner { get; }

        string ParentName { get; }

    }
}

