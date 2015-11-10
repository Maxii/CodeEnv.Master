// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileTube.cs
// A Weapon Mount for fire and forget Weapons like Missiles.
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
/// A Weapon Mount for fire and forget Weapons like Missiles.
/// </summary>
public class MissileTube : AWeaponMount {

    /// <summary>
    /// The visible mouth protrusion of the tube. 
    /// Note: The location of the muzzle should be directly over the mouth so the
    /// launch direction can always be determined.
    /// </summary>
    //[FormerlySerializedAs("tubeMouth")]
    [SerializeField]
    private Transform _tubeMouth = null;

    public override Vector3 MuzzleFacing { get { return (_muzzle.position - _tubeMouth.position).normalized; } }

    protected override void Validate() {
        base.Validate();
        D.Assert(_tubeMouth != null);
    }

    /// <summary>
    /// Trys to develop a firing solution from this WeaponMount to the provided target. If successful, returns <c>true</c> and provides the
    /// firing solution, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <param name="firingSolution"></param>
    /// <returns></returns>
    public override bool TryGetFiringSolution(IElementAttackableTarget enemyTarget, out WeaponFiringSolution firingSolution) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(Weapon.Owner));

        if (!ConfirmInRange(enemyTarget)) {
            //D.Log("{0}.CheckFiringSolution({1}) has determined target is out of range.", Name, enemyTarget.FullName);
            firingSolution = null;
            return false;
        }
        firingSolution = new WeaponFiringSolution(Weapon, enemyTarget);
        return true;
    }

    /// <summary>
    /// Confirms the provided enemyTarget is in range prior to launching the weapon's ordnance.
    /// </summary>
    /// <param name="enemyTarget">The target.</param>
    /// <returns></returns>
    public override bool ConfirmInRange(IElementAttackableTarget enemyTarget) {
        float distanceToPushover = TempGameValues.__ReqdMissileTravelDistanceBeforePushover;
        Vector3 launchDirection = MuzzleFacing;
        Vector3 vectorToPushover = launchDirection * distanceToPushover;
        Vector3 launchPosition = MuzzleLocation;
        Vector3 pushoverPosition = launchPosition + vectorToPushover;

        Vector3 vectorToTargetFromPushover = enemyTarget.Position - pushoverPosition;
        float targetDistanceFromPushover = vectorToTargetFromPushover.magnitude;
        return distanceToPushover + targetDistanceFromPushover < Weapon.RangeDistance;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

