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

/// <summary>
/// A Weapon Mount for fire and forget Weapons like Missiles.
/// </summary>
public class MissileTube : AWeaponMount {

    /// <summary>
    /// The visible mouth protrusion of the tube. 
    /// Note: The location of the muzzle should be directly over the mouth so the
    /// launch direction can always be determined.
    /// </summary>
    public Transform tubeMouth;

    public override Vector3 MuzzleFacing { get { return (muzzle.position - tubeMouth.position).normalized; } }

    protected override void Validate() {
        base.Validate();
        D.Assert(tubeMouth != null);
    }

    /// <summary>
    /// Checks the firing solution of this weapon on the enemyTarget. Returns <c>true</c> if the target
    /// fits within the weapon's firing solution, aka within range and can be acquired, if required.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    public override bool CheckFiringSolution(IElementAttackableTarget enemyTarget) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(Weapon.Owner));

        float distanceToPushover = TempGameValues.__ReqdMissileTravelDistanceBeforePushover;
        Vector3 launchDirection = MuzzleFacing;
        Vector3 vectorToPushover = launchDirection * distanceToPushover;
        Vector3 pushoverPosition = MuzzleLocation + vectorToPushover;

        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTargetFromPushover = targetPosition - pushoverPosition;
        float targetDistanceFromPushover = vectorToTargetFromPushover.magnitude;
        if (distanceToPushover + targetDistanceFromPushover > Weapon.RangeDistance) {
            D.Log("{0}.CheckFiringSolution({1}) has determined target is out of range.", Name, enemyTarget.FullName);
            return false;
        }
        return true;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

