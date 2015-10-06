// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWeaponMount.cs
// Abstract base class for a mount used for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for a mount used for Weapons.
/// </summary>
public abstract class AWeaponMount : AMount, IWeaponMount {

    public Transform muzzle;

    [Tooltip("The folder that holds currently deployed ordnance.")]
    public GameObject firedOrdnanceFolder;

    public virtual string Name { get { return transform.name; } }

    public AWeapon Weapon { get; set; }

    /// <summary>
    /// The folder that holds currently deployed ordnance.
    /// </summary>
    public GameObject FiredOrdnanceFolder { get { return firedOrdnanceFolder; } }

    /// <summary>
    /// The location of the weapon's muzzle in world space coordinates. 
    /// Should be used only for positioning fired ordnance and associated muzzle effect.
    /// </summary>
    public Vector3 MuzzleLocation { get { return muzzle.position; } }

    /// <summary>
    /// The current facing of the muzzle in world space coordinates.
    /// </summary>
    public abstract Vector3 MuzzleFacing { get; }

    public MountSlotID SlotID { get; set; }

    protected override void Validate() {
        base.Validate();
        D.Assert(muzzle != null);
        D.Assert(firedOrdnanceFolder != null);
    }

    /// <summary>
    /// Checks the firing solution of the mount's weapon against the enemyTarget. Returns <c>true</c> if the target
    /// fits within the weapon's firing solution, aka within range and can be acquired (beared upon).
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    public abstract bool CheckFiringSolution(IElementAttackableTarget enemyTarget);

}

