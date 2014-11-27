// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalTarget.cs
// Interface for a target that can take damage and die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for a target that can take damage and die.
    /// </summary>
    [Obsolete]
    public interface IMortalTarget : IOwnedTarget, INavigableTarget {

        /// <summary>
        /// Occurs when this mortal target has died. Intended for external
        /// notification to others that have targeted this mortal target as the
        /// IMortalTarget interface provides only limited access to the model.
        /// </summary>
        event Action<IMortalTarget> onTargetDeathOneShot;

        /// <summary>
        /// Flag indicating whether the MortalItem is alive and operational.
        /// </summary>
        bool IsAlive { get; }

        void TakeHit(CombatStrength attackerWeaponStrength);

        string ParentName { get; }

    }
}

