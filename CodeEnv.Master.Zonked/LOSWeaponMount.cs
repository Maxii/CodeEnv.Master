// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LOSWeaponMount.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
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
public class LOSWeaponMount : AWeaponMount, ILOSWeaponMount {

    private static LayerMask _defaultOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);

    /// <summary>
    /// The max traverse rate of this mount in degrees per hour.
    /// </summary>
    private static float _maxTraverseRate = 270F;

    /// <summary>
    /// The _allowed deviation in degrees when determining when a traverse is completed.
    /// </summary>
    private static float _allowedTraversalDeviation = .1F;

    /// <summary>
    /// The total range this weapon mount can traverse from its amidship facing in degrees.
    /// </summary>
    private static float _traverseRange = 60F;

    /// <summary>
    /// Occurs when [on traverse completed]. 
    /// </summary>
    public event Action onTraverseCompleted;

    private void OnTraverseCompleted() {
        if (onTraverseCompleted != null) {
            onTraverseCompleted();
        }
    }

    /// <summary>
    /// The current world space coordinate facing of the weapon when the mount is in the amidship position,
    /// aka when its current traverse rotation is 0 degrees. This world space facing direction when amidship is affected
    /// by the current heading of the element it is attached too.
    /// Used to determine whether a target's bearing is within the firing envelope of the weapon.
    /// </summary>
    public Vector3 CurrentAmidshipFacing { get { return transform.TransformDirection(_amidshipLocalFacing); } }

    private Vector3 _amidshipLocalFacing;
    private GameTime _gameTime;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _gameTime = GameTime.Instance;
        _amidshipLocalFacing = transform.InverseTransformDirection(transform.forward);
    }

    /// <summary>
    /// Traverses this mount's weapon to point at targetDirection in world space coordinates.
    /// </summary>
    /// <param name="targetDirection">The target direction.</param>
    public void TraverseTo(Vector3 targetDirection) {
        Traverse(targetDirection, allowedTime: 5F);
    }

    /// <summary>
    /// Traverses this mount's weapon to point in the intended direction in world space coordinates.
    /// </summary>
    /// <param name="intendedDirection">The intended direction.</param>
    /// <param name="allowedTime">The allowed time before an error is thrown.</param>
    private void Traverse(Vector3 intendedDirection, float allowedTime = Mathf.Infinity) {
        intendedDirection.ValidateNormalized();

        if (intendedDirection.IsSameDirection(CurrentFacing, _allowedTraversalDeviation)) {
            D.Log("{0} ignoring a very small Traverse request of {1:0.0000} degrees.", Name, Vector3.Angle(intendedDirection, CurrentFacing));
            OnTraverseCompleted();
            return;
        }

        D.Log("{0} received Traverse to {1}.", Name, intendedDirection);
        if (_traverseJob != null && _traverseJob.IsRunning) {
            _traverseJob.Kill();
            // onJobComplete will run next frame so placed cancelled notice here
            D.Error("{0}'s previous Traverse Job has been cancelled.", Name);   // if this happens, no onTraverseCompleted will be returned for cancelled traverse
        }

        _traverseJob = new Job(ExecuteTraversal(intendedDirection, allowedTime), toStart: true, onJobComplete: (jobWasKilled) => {
            if (!jobWasKilled) {
                D.Log("{0}'s traverse to {1} complete.  Deviation = {2:0.00} degrees.", Name, intendedDirection, Vector3.Angle(CurrentFacing, intendedDirection));
                OnTraverseCompleted();
            }
        });
    }

    private Job _traverseJob;

    /// <summary>
    /// Coroutine that executes a traverse without overshooting.
    /// </summary>
    /// <param name="intendedDirection">The intended direction to traverse too.</param>
    /// <param name="allowedTime">The allowed time in GameTimeSeconds.</param>
    /// <returns></returns>
    private IEnumerator ExecuteTraversal(Vector3 intendedDirection, float allowedTime) {
        int previousFrameCount = Time.frameCount - 1;   // makes initial framesSinceLastPass = 1
        int cumFrameCount = 0;
        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _maxTraverseRate * GameTime.HoursPerSecond;
        D.Log("{0} initiating traverse to {1} at {2:0.} degrees/hour.", Name, intendedDirection, _maxTraverseRate);
        float cumTime = 0F;
        while (!CurrentFacing.IsSameDirection(intendedDirection)) {
            int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
            cumFrameCount += framesSinceLastPass;   // IMPROVE adjust frameCount for pausing?
            previousFrameCount = Time.frameCount;
            float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.GameSpeedAdjustedDeltaTimeOrPaused * framesSinceLastPass;
            Vector3 newDirection = Vector3.RotateTowards(CurrentFacing, intendedDirection, allowedTurn, maxMagnitudeDelta: 1F);
            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
            transform.rotation = Quaternion.LookRotation(newDirection);
            cumTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused; // WARNING: works only with yield return null;
            D.Assert(cumTime < allowedTime, "CumTime {0} > AllowedTime {1}.".Inject(cumTime, allowedTime));
            yield return null; // new WaitForSeconds(0.5F); // new WaitForFixedUpdate();
        }
        D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs. FrameCount = {2}.", Name, cumTime, cumFrameCount);
    }


    /// <summary>
    /// Checks the firing solution of this weapon on the enemyTarget. Returns <c>true</c> if the target
    /// fits within the weapon's firing solution, aka within range and can be acquired. if reqd.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    public override bool CheckFiringSolution(AWeapon weapon, IElementAttackableTarget enemyTarget) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(weapon.Owner));

        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTarget = targetPosition - MuzzleLocation;
        float targetDistance = vectorToTarget.magnitude;
        if (targetDistance > weapon.RangeDistance) {
            D.Log("{0}.CheckFiringSolution({1}) has determined target is out of range.", Name, enemyTarget.FullName);
            return false;
        }

        Vector3 targetDirection = vectorToTarget.normalized;
        float reqdTraversal;
        bool canTraverseToTarget = UnityUtility.AreDirectionsWithinTolerance(CurrentAmidshipFacing, targetDirection, _traverseRange, out reqdTraversal);
        if (!canTraverseToTarget) {
            D.Log("{0}.CheckFiringSolution({1}) has determined target is beyond traversal range. Reqd traversal = {2:0.} degrees.", Name, enemyTarget.FullName, reqdTraversal);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks the line of sight from this weaponMount to the provided enemy target, returning <c>true</c>
    /// if 1) the target is within range of the weapon located on this mount, 2) the target is within the traversal range
    /// of this mount and 3) the LOS is clear to the target, otherwise <c>false</c>. If <c>false</c> and the interference is from
    /// another enemy target, then interferingEnemyTgt is assigned that target. Otherwise, interferingEnemyTgt
    /// will always be null. In route ordnance does not interfere with this LOS.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <param name="interferingEnemyTgt">The interfering enemy target, if any.</param>
    /// <returns></returns>
    public bool CheckLineOfSight(AWeapon weapon, IElementAttackableTarget enemyTarget, out IElementAttackableTarget interferingEnemyTgt) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(weapon.Owner));
        interferingEnemyTgt = null;

        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTarget = targetPosition - MuzzleLocation;
        float targetDistance = vectorToTarget.magnitude;
        D.Assert(targetDistance <= weapon.RangeDistance);   // should have already been checked by CheckFiringSolution()

        Vector3 targetDirection = vectorToTarget.normalized;
        float reqdTraversal;
        bool canTraverseToTarget = UnityUtility.AreDirectionsWithinTolerance(CurrentAmidshipFacing, targetDirection, _traverseRange, out reqdTraversal);
        D.Assert(canTraverseToTarget);  // should have already been checked by CheckFiringSolution()

        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, targetDirection, out hitInfo, targetDistance, _defaultOnlyLayerMask)) {
            var targetHit = hitInfo.transform.gameObject.GetSafeInterface<IElementAttackableTarget>();
            if (targetHit != null) {
                if (targetHit == enemyTarget) {
                    D.Log("{0}.CheckLineOfSightTo({1}) found its target.", Name, enemyTarget.FullName);
                    return true;
                }
                if (targetHit.Owner.IsEnemyOf(weapon.Owner)) {
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

    protected override void Cleanup() {
        if (_traverseJob != null) {
            _traverseJob.Kill();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

