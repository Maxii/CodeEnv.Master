// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponRangeMonitor.cs
// Interface for access to a WeaponRangeMonitor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for access to a WeaponRangeMonitor.
    /// </summary>
    public interface IWeaponRangeMonitor : IRangedEquipmentMonitor {

        IUnitElementItem ParentItem { set; get; }

        /// <summary>
        /// Checks the line of sight from this monitor (element) to the provided enemy target, returning <c>true</c>
        /// if the LOS is clear to the target, otherwise <c>false</c>. If <c>false</c> and the interference is from 
        /// another enemy target, then interferingEnemyTgt is assigned that target. Otherwise, interferingEnemyTgt
        /// will always be null. In route ordnance does not interfere with this LOS.
        /// </summary>
        /// <param name="enemyTarget">The target.</param>
        /// <param name="interferingEnemyTgt">The interfering enemy target.</param>
        /// <returns></returns>
        bool CheckLineOfSightTo(IElementAttackableTarget enemyTarget, out IElementAttackableTarget interferingEnemyTgt);

    }
}

