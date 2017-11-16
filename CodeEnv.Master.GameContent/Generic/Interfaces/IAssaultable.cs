﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAssaultable.cs
// Interface for Elements that can be assaulted (invaded) by ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Elements that can be assaulted (invaded) by ships.
    /// </summary>
    public interface IAssaultable : IShipNavigableDestination, ISensorDetectable, IAttackable {

        new string DebugName { get; }

        new Vector3 Position { get; }

        bool IsVisualDetailDiscernibleToUser { get; }

        bool IsAssaultAllowedBy(Player player);

        bool AttemptAssault(Player player, DamageStrength strength, string __assaulterName);

        void __ChangeOwner(Player newOwner);

    }
}

