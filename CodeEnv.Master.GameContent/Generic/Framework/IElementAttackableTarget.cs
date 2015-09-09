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

        void TakeHit(DamageStrength attackerStrength);

        bool IsVisualDetailDiscernibleToUser { get; }

    }
}

