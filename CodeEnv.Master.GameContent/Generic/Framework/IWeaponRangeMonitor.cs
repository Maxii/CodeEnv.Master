// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponRangeMonitor.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IWeaponRangeMonitor : IRangedEquipmentMonitor {

        IUnitElementItem ParentItem { set; }

        void Add(AWeapon weapon);

        /// <summary>
        /// Removes the specified weapon. Returns <c>true</c> if this monitor
        /// is still in use (has weapons remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <returns></returns>
        bool Remove(AWeapon weapon);

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

