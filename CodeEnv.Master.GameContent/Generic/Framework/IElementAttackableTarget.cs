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

    /// <summary>
    /// Interface for targets that can be attacked by unit elements.
    /// </summary>
    public interface IElementAttackableTarget : INavigableTarget {

        event Action<IItem> onOwnerChanged;

        event Action<IMortalItem> onDeathOneShot;

        Player Owner { get; }

        bool IsOperational { get; }

        void TakeHit(CombatStrength attackerWeaponStrength);

    }
}

