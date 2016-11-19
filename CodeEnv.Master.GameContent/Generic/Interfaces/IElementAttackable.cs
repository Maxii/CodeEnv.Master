// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementAttackable.cs
// Interface for targets that can be attacked by unit elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for targets that can be attacked by unit elements.
    /// </summary>
    public interface IElementAttackable : IShipNavigable, ISensorDetectable, IAttackable {

        new string FullName { get; }

        new Vector3 Position { get; }

        bool IsVisualDetailDiscernibleToUser { get; }

        void TakeHit(DamageStrength attackerStrength);

    }
}

