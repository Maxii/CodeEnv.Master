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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for a mount on a hull used for weapons.
    /// </summary>
    public interface IWeaponMount {

        string DebugName { get; }

        /// <summary>
        /// The location of the mounted weapon's muzzle in world space coordinates.
        /// </summary>
        Vector3 MuzzleLocation { get; }

        /// <summary>
        /// The current facing of the muzzle in world space coordinates.
        /// </summary>
        Vector3 MuzzleFacing { get; }

        /// <summary>
        /// The Muzzle Transform. Used as the parent of the BeamOrdnance gameObject while being fired.
        /// </summary>
        Transform Muzzle { get; }

        AWeapon Weapon { get; set; }

        bool TryGetFiringSolution(IElementAttackable enemyTarget, out WeaponFiringSolution firingSolution);

        /// <summary>
        /// Confirms the provided enemyTarget is in range PRIOR to launching the weapon's ordnance.
        /// </summary>
        /// <param name="enemyTarget">The target.</param>
        /// <returns></returns>
        bool ConfirmInRangeForLaunch(IElementAttackable enemyTarget);

    }
}

