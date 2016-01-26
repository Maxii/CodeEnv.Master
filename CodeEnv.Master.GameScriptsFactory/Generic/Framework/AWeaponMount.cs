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
using UnityEngine.Serialization;

/// <summary>
/// Abstract base class for a mount used for Weapons.
/// </summary>
public abstract class AWeaponMount : AMount, IWeaponMount {

    //[FormerlySerializedAs("muzzle")]
    [SerializeField]
    protected Transform _muzzle = null;

    public virtual string Name { get { return transform.name; } }

    private AWeapon _weapon;
    public AWeapon Weapon {
        get { return _weapon; }
        set {
            D.Assert(_weapon == null);  // only happens once
            _weapon = value;
            WeaponPropSetHandler();
        }
    }

    /// <summary>
    /// The location of the weapon's muzzle in world space coordinates. 
    /// Should be used only for positioning fired ordnance and associated muzzle effect.
    /// </summary>
    public Vector3 MuzzleLocation { get { return _muzzle.position; } }

    /// <summary>
    /// The current facing of the muzzle in world space coordinates.
    /// </summary>
    public abstract Vector3 MuzzleFacing { get; }

    public MountSlotID SlotID { get; set; } // OPTIMIZE Not currently used

    protected override void Validate() {
        base.Validate();
        D.Assert(_muzzle != null);
    }

    /// <summary>
    /// Trys to develop a firing solution from this WeaponMount to the provided target. If successful, returns <c>true</c> and provides the
    /// firing solution, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <param name="firingSolution"></param>
    /// <returns></returns>
    public abstract bool TryGetFiringSolution(IElementAttackableTarget enemyTarget, out WeaponFiringSolution firingSolution);

    /// <summary>
    /// Confirms the provided enemyTarget is in range PRIOR to launching the weapon's ordnance.
    /// </summary>
    /// <param name="enemyTarget">The target.</param>
    /// <returns></returns>
    public abstract bool ConfirmInRangeForLaunch(IElementAttackableTarget enemyTarget);

    #region Event and Property Change Handlers

    protected virtual void WeaponPropSetHandler() { }

    #endregion

}

