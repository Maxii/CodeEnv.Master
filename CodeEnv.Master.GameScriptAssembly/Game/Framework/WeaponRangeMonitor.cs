// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeMonitor.cs
// Detects IDetectable Items that enter and exit the range of its weapons and notifies each weapon of such.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Detects IDetectable Items that enter and exit the range of its weapons and notifies each weapon of such.
/// TODO Account for a diploRelations change with an owner.
/// <remarks>This WeaponRangeMonitor assumes that Short, Medium and LongRange weapons all detect
/// IDetectable items using the element's "Proximity Detectors" that are always operational. They donot rely on Sensors.</remarks>
/// </summary>
public class WeaponRangeMonitor : ARangedEquipmentMonitor<AWeapon, IUnitElementItem>, IWeaponRangeMonitor {

    private static LayerMask _defaultOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);

    public override void Add(AWeapon weapon) {
        base.Add(weapon);
    }

    public override bool Remove(AWeapon weapon) {
        return base.Remove(weapon);
    }

    /// <summary>
    /// Checks the line of sight from this monitor (element) to the provided enemy target, returning <c>true</c>
    /// if the LOS is clear to the target, otherwise <c>false</c>. If <c>false</c> and the interference is from 
    /// another enemy target, then interferingEnemyTgt is assigned that target. Otherwise, interferingEnemyTgt
    /// will always be null. In route ordnance does not interfere with this LOS.
    /// </summary>
    /// <param name="enemyTarget">The target.</param>
    /// <param name="interferingEnemyTgt">The interfering enemy target.</param>
    /// <returns></returns>
    public bool CheckLineOfSightTo(IElementAttackableTarget enemyTarget, out IElementAttackableTarget interferingEnemyTgt) {
        D.Assert(enemyTarget.IsOperational);
        interferingEnemyTgt = null;

        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTarget = targetPosition - transform.position;
        float targetDistance = vectorToTarget.magnitude;
        Vector3 targetDirection = vectorToTarget.normalized;
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, targetDirection, out hitInfo, targetDistance, _defaultOnlyLayerMask)) {
            var targetHit = hitInfo.transform.gameObject.GetSafeInterface<IElementAttackableTarget>();
            if (targetHit != null) {
                if (targetHit == enemyTarget) {
                    //D.Log("{0}.CheckLineOfSightTo({1}) found its target.", Name, enemyTarget.FullName);
                    return true;
                }
                if (targetHit.Owner.IsEnemyOf(Owner)) {
                    interferingEnemyTgt = targetHit;
                    D.Log("{0}.CheckLineOfSightTo({1}) found interfering enemy target {2}.", Name, enemyTarget.FullName, interferingEnemyTgt.FullName);
                    return false;
                }
                D.Log("{0}.CheckLineOfSightTo({1}) found interfering non-enemy target {2}.", Name, enemyTarget.FullName, targetHit.FullName);
                return false;
            }
            D.Warn("{0}.CheckLineOfSightTo() didn't find target {1} but found {2}.", Name, enemyTarget.FullName, hitInfo.transform.name);
            return false;
        }
        D.Warn("{0}.CheckLineOfSightTo({1}) didn't find anything.", Name, enemyTarget.FullName);    // shouldn't happen?
        return false;
    }

    protected override void AssignMonitorTo(AWeapon pieceOfEquipment) {
        pieceOfEquipment.RangeMonitor = this;
    }

    protected override void RemoveMonitorFrom(AWeapon pieceOfEquipment) {
        pieceOfEquipment.RangeMonitor = null;
    }

    protected override void OnTargetBecomesNonEnemy(IElementAttackableTarget nonEnemyTarget) {
        base.OnTargetBecomesNonEnemy(nonEnemyTarget);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
    }

    protected override void OnEnemyTargetInRange(IElementAttackableTarget enemyTarget) {
        base.OnEnemyTargetInRange(enemyTarget);
        _equipmentList.ForAll(weap => weap.OnEnemyTargetInRangeChanged(enemyTarget, isInRange: true));
    }

    protected override void OnEnemyTargetOutOfRange(IElementAttackableTarget enemyTarget) {
        base.OnEnemyTargetOutOfRange(enemyTarget);
        _equipmentList.ForAll(weap => weap.OnEnemyTargetInRangeChanged(enemyTarget, isInRange: false));
    }

    protected override void OnParentOwnerChanged(IItem parentItem) {
        base.OnParentOwnerChanged(parentItem);
        _equipmentList.ForAll(weap => weap.CheckActiveOrdnanceTargeting());
    }

    protected override float RefreshRangeDistance() {
        var operationalWeapons = _equipmentList.Where(weap => weap.IsOperational);
        return operationalWeapons.Any() ? operationalWeapons.First().RangeDistance : Constants.ZeroF;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

