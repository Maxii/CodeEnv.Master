// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalTarget.cs
// Interface for a target that is mortal.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a target that is mortal.
    /// </summary>
    public interface IMortalTarget : IDestinationTarget {

        event Action<IMortalModel> onItemDeath;

        event Action<IMortalModel> onOwnerChanged;

        /// <summary>
        /// Flag indicating whether the MortalItem is dead.
        /// </summary>
        bool IsAlive { get; }

        void TakeHit(CombatStrength attackerWeaponStrength);

        IPlayer Owner { get; }

        float MaxWeaponsRange { get; }

        string ParentName { get; }

    }
}

