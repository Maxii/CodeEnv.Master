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

        event Action<IMortalItem> onDeathOneShot;

        event Action<IItem> onOwnerChanged;

        IPlayer Owner { get; }

        bool IsAliveAndOperating { get; }

        void TakeHit(CombatStrength attackerWeaponStrength);

    }
}

