// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponMount.cs
// Interface for a mount on a hull used for weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for a mount on a hull used for weapons.
    /// </summary>
    public interface IWeaponMount {

        string Name { get; }

        /// <summary>
        /// The location of the mounted weapon's muzzle in world space coordinates.
        /// </summary>
        Vector3 MuzzleLocation { get; }

        /// <summary>
        /// The current facing of the muzzle in world space coordinates.
        /// </summary>
        Vector3 MuzzleFacing { get; }

        AWeapon Weapon { get; set; }

        /// <summary>
        /// Checks the firing solution of this mount's weapon on the enemyTarget. Returns <c>true</c> if the target
        /// fits within the weapon's firing solution, aka within range and can be beared upon, if reqd.
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <returns></returns>
        bool CheckFiringSolution(IElementAttackableTarget enemyTarget);

        GameObject FiredOrdnanceFolder { get; }


    }
}

