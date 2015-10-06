// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileWeaponMount.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
[Obsolete]
public class MissileWeaponMount : AWeaponMount {

    /// <summary>
    /// Checks the firing solution of this weapon on the enemyTarget. Returns <c>true</c> if the target
    /// fits within the weapon's firing solution, aka within range and can be acquired, if required.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    public override bool CheckFiringSolution(AWeapon weapon, IElementAttackableTarget enemyTarget) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(weapon.Owner));

        float distanceToPushover = TempGameValues.__ReqdMissileTravelDistanceBeforePushover;
        Vector3 vectorToPushover = CurrentFacing * distanceToPushover;
        Vector3 pushoverPosition = MuzzleLocation + vectorToPushover;

        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTargetFromPushover = targetPosition - pushoverPosition;
        float targetDistanceFromPushover = vectorToTargetFromPushover.magnitude;
        if (distanceToPushover + targetDistanceFromPushover > weapon.RangeDistance) {
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

