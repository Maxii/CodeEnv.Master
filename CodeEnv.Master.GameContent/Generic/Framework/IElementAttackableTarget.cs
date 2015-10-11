// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementAttackableTarget.cs
// Interface for targets that can be attacked by unit elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for targets that can be attacked by unit elements.
    /// </summary>
    public interface IElementAttackableTarget : INavigableTarget, ISensorDetectable {

        event Action<IMortalItem> onDeathOneShot;

        new string FullName { get; }

        new string DisplayName { get; }

        new Vector3 Position { get; }

        /// <summary>
        /// Called by the ordnanceFired to notify its target of the launch
        /// of the ordnance. This workaround is necessary in cases where the ordnance is
        /// launched inside the target's ActiveCountermeasureRangeMonitor
        /// collider sphere since GameObjects instantiated inside a collider are
        /// not detected by OnTriggerEnter(). The target will only take action on
        /// this FYI if it determines that the ordnance will not be detected by one or
        /// more of its monitors.
        /// Note: Obsolete as all interceptable ordnance has a rigidbody which is detected by this monitor when the 
        /// ordnance moves, even if it first appears inside the monitor's collider.
        /// </summary>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        [Obsolete]
        void OnFiredUponBy(IInterceptableOrdnance ordnanceFired);

        void TakeHit(DamageStrength attackerStrength);

        bool IsVisualDetailDiscernibleToUser { get; }

    }
}

