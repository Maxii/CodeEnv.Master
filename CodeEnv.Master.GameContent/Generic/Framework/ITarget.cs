// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITarget.cs
// Interface for a MortalItem that is an attack target of another Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a MortalItem that is an attack target of another Item.
    /// </summary>
    public interface ITarget : IDestinationTarget {

        event Action<ITarget> onItemDeath;

        bool IsDead { get; }

        void TakeDamage(float damage);

        IPlayer Owner { get; }

    }
}

