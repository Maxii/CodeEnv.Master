// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ILOSWeaponMount.cs
// Interface for a weapon mount on a hull used for line of sight weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for a weapon mount on a hull used for line of sight weapons.
    /// </summary>
    public interface ILOSWeaponMount : IWeaponMount {

        /// <summary>
        /// Traverses the mount to point at the target's world space position.
        /// </summary>
        /// <param name="targetPosition">The target position.</param>
        void TraverseTo(Vector3 targetPosition);

        /// <summary>
        /// Checks the line of sight from this LOSWeaponMount to the provided enemy target, returning <c>true</c>
        /// if their is a clear line of sight to the target, otherwise <c>false</c>. If <c>false</c> and the LOS interference is from
        /// another enemy target, then interferingEnemyTgt is assigned that target. Otherwise, interferingEnemyTgt
        /// will always be null. In route ordnance does not interfere with this LOS check.
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <param name="interferingEnemyTgt">The interfering enemy target, if any.</param>
        /// <returns></returns>
        bool CheckLineOfSight(IElementAttackableTarget enemyTarget, out IElementAttackableTarget interferingEnemyTgt);

    }
}

