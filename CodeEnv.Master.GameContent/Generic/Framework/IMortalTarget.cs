// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITarget.cs
// Interface for a MortalItem that can be an attack target of a Unit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a MortalItem that can be an attack target of a Unit.
    /// </summary>
    public interface IMortalTarget : IDestination {

        event Action<IMortalTarget> onItemDeath;

        event Action<IMortalTarget> onOwnerChanged;

        bool IsDead { get; }

        void TakeDamage(float damage);

        IPlayer Owner { get; }

        string ParentName { get; }

        float MaxWeaponsRange { get; }

    }
}

