// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LOSTurret.cs
// A Turret Weapon Mount for Line Of Sight Weapons like Beams and Projectiles.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

/// <summary>
/// A Turret Weapon Mount for Line Of Sight Weapons like Beams and Projectiles.
/// </summary>
public class LOSTurret : AWeaponMount, ILOSWeaponMount {

    private const string DebugNameFormat = "{0}.{1}.{2}";

    /// <summary>
    /// The barrel's maximum elevation angle. 
    /// This value when its sign is inverted and applied to a Euler angle generates a rotation that points straight out from the turret.
    /// </summary>
    private const float BarrelMaxElevationAngle = 90F;

    /// <summary>
    /// The rotation rate of the hub of this turret in degrees per hour.
    /// </summary>
    private const float HubRotationRate = 270F;

    /// <summary>
    /// The elevation change rate of the barrel of this turret in degrees per hour.
    /// </summary>
    private const float BarrelElevationRate = 90F;

    /// <summary>
    /// The maximum number of degrees the hub should have to rotate when traversing.
    /// <remarks>Logs during traverse shows Quaternion always goes shortest route when rotating.</remarks>
    /// </summary>
    private const float HubMaxRotationTraversal = 180F;

    /// <summary>
    /// The maximum number of degrees the barrel should have to elevate when traversing.
    /// <remarks>Logs during traverse shows Quaternion always goes shortest route when rotating.</remarks>
    /// </summary>
    private const float BarrelMaxElevationTraversal = 90F;

    /// <summary>
    /// The maximum elevation of the barrel of this turret which is the midpoint in its elevation traverse range.
    /// <remarks>Barrel x elevation angle to face outward must be negative due to the orientation of the local x axis of the barrel prefab.</remarks>
    /// </summary>
    private static Quaternion _barrelMaxElevation = Quaternion.Euler(new Vector3(-BarrelMaxElevationAngle, Constants.ZeroF, Constants.ZeroF));

    private static LayerMask _defaultOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default);

    //[FormerlySerializedAs("hub")]
    [SerializeField]
    private Transform _hub = null;

    //[FormerlySerializedAs("barrel")]
    [SerializeField]
    private Transform _barrel = null;

    private string _debugName;
    public override string DebugName {
        get {
            if (Weapon == null) {
                return base.DebugName;
            }
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(Weapon.Name, GetType().Name, SlotID.GetValueName());
            }
            return _debugName;
        }
    }

    public override Vector3 MuzzleFacing { get { return (Muzzle.position - _barrel.position).normalized; } }

    public new ALOSWeapon Weapon {
        get { return base.Weapon as ALOSWeapon; }
        set { base.Weapon = value; }
    }

    private bool ShowDebugLog { get { return Weapon != null ? Weapon.ShowDebugLog : true; } }

    /// <summary>
    /// The inaccuracy of this Turret when traversing in degrees.
    /// Affects both hub rotation and barrel elevation.
    /// </summary>
    private float AllowedTraverseInaccuracy { get { return UnityConstants.AngleEqualityPrecision; } }

#pragma warning disable 0414    // OPTIMIZE

    /// <summary>
    /// The elevation the turret barrel rests at when not in use. 
    /// Currently initialized to facing forward in the case of turrets on top, bottom, port and starboard,
    /// and facing downward for any forward and aft turrets. Not currently used. // IMPROVE
    /// </summary>
    private Quaternion _barrelRestElevation;

#pragma warning restore 0414

    /// <summary>
    /// The allowed number of degrees the barrel elevation angle can deviate from maximum.
    /// Used to determine when the barrel is within its traversal range.
    /// </summary>
    private float _allowedBarrelElevationAngleDeviationFromMax;
    private GameTime _gameTime;
    private GameManager _gameMgr;
    private JobManager _jobMgr;
    private Job _traverseJob;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _gameTime = GameTime.Instance;
        _gameMgr = GameManager.Instance;
        _jobMgr = JobManager.Instance;
        _barrelRestElevation = _barrel.localRotation;
    }

    protected override void Validate() {
        base.Validate();
        D.AssertNotNull(_hub);
        D.AssertNotNull(_barrel);
    }

    /// <summary>
    /// Initializes the barrel elevation settings based off of the provided minimum barrel elevation angle.
    /// </summary>
    /// <param name="minBarrelElevationAngle">The minimum barrel elevation angle.</param>
    public void InitializeBarrelElevationSettings(float minBarrelElevationAngle) {
        _allowedBarrelElevationAngleDeviationFromMax = BarrelMaxElevationAngle - minBarrelElevationAngle;
    }

    /// <summary>
    /// Tries to develop a firing solution from this WeaponMount to the provided target. If successful, returns <c>true</c> and provides the
    /// firing solution, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <param name="firingSolution"></param>
    /// <returns></returns>
    public override bool TryGetFiringSolution(IElementAttackable enemyTarget, out WeaponFiringSolution firingSolution) {
        D.Assert(enemyTarget.IsOperational);
        if (!enemyTarget.IsAttackAllowedBy(Weapon.Owner)) {
            bool hasAccessToAttackTgtOwner = enemyTarget.IsOwnerAccessibleTo(Weapon.Owner);
            D.Error("{0} can no longer attack {1}. Has access to attackTgt owner = {2}.", DebugName, enemyTarget.DebugName, hasAccessToAttackTgtOwner);
            // 3.17.17 BUG: occurred while everyone is war enemy so presumably we lost access to the attackTgt owner. If so, why wasn't
            // the enemyTgt removed from AWeapon._attackableEnemyTgts when Monitor received InfoAccessChg event?
        }
        D.Assert(enemyTarget.IsAttackAllowedBy(Weapon.Owner));

        firingSolution = null;
        if (!ConfirmInRangeForLaunch(enemyTarget)) {
            //D.Log(ShowDebugLog, "{0}: Target {1} is out of range.", DebugName, enemyTarget.DebugName);
            return false;
        }

        Vector3 targetPosition = enemyTarget.Position;
        Quaternion reqdHubRotation, reqdBarrelElevation;
        bool canTraverseToTarget = TryCalcTraverse(targetPosition, out reqdHubRotation, out reqdBarrelElevation);
        if (!canTraverseToTarget) {
            //D.Log(ShowDebugLog, "{0}: Target {1} is out of traverse range.", DebugName, enemyTarget.DebugName);
            return false;
        }

        bool isLosClear = CheckLineOfSight(enemyTarget);
        if (!isLosClear) {
            return false;
        }
        firingSolution = new LosWeaponFiringSolution(Weapon, enemyTarget, reqdHubRotation, reqdBarrelElevation);
        return true;
    }

    /// <summary>
    /// Confirms the provided enemyTarget is in range prior to launching the weapon's ordnance.
    /// </summary>
    /// <param name="enemyTarget">The target.</param>
    /// <returns></returns>
    public override bool ConfirmInRangeForLaunch(IElementAttackable enemyTarget) {
        float weaponRange = Weapon.RangeDistance;
        return Vector3.SqrMagnitude(enemyTarget.Position - _hub.position) < weaponRange * weaponRange;
    }

    /// <summary>
    /// Checks the line of sight from this LOSWeaponMount to the provided enemy target, returning <c>true</c>
    /// if there is a clear line of sight in the direction of the target, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    private bool CheckLineOfSight(IElementAttackable enemyTarget) {
        Vector3 turretPosition = _hub.position;
        Vector3 vectorToTarget = enemyTarget.Position - turretPosition;
        Vector3 targetDirection = vectorToTarget.normalized;
        float targetDistance = vectorToTarget.magnitude;
        RaycastHit raycastHitInfo;
        if (Physics.Raycast(turretPosition, targetDirection, out raycastHitInfo, targetDistance, _defaultOnlyLayerMask)) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var attackableTgtEncountered = raycastHitInfo.transform.GetComponent<IElementAttackable>();
            Profiler.EndSample();

            if (attackableTgtEncountered != null) {
                if (attackableTgtEncountered == enemyTarget) {
                    //D.Log(ShowDebugLog, "{0}: CheckLineOfSight({1}) found its target.", DebugName, enemyTarget.DebugName);
                    return true;
                }
                if (attackableTgtEncountered.IsAttackAllowedBy(Weapon.Owner)) {
                    D.Log(ShowDebugLog, "{0}: CheckLineOfSight({1}) found interfering attackable target {2} on {3}.", DebugName, enemyTarget.DebugName, attackableTgtEncountered.DebugName, _gameTime.CurrentDate);
                    return false;
                }
                D.Log(ShowDebugLog, "{0}: CheckLineOfSight({1}) found interfering non-attackable target {2} on {3}.", DebugName, enemyTarget.DebugName, attackableTgtEncountered.DebugName, _gameTime.CurrentDate);
                return false;
            }
            D.Log(ShowDebugLog, "{0}: CheckLineOfSight({1}) didn't find target but found {2} on {3}.", DebugName, enemyTarget.DebugName, raycastHitInfo.transform.name, _gameTime.CurrentDate);
            return false;
        }
        //D.Log(ShowDebugLog, "{0}: CheckLineOfSight({1}) didn't find anything. Date: {2}.", DebugName, enemyTarget.DebugName, _gameTime.CurrentDate);
        return true;
    }

    /// <summary>
    /// Traverses the mount to point at the target defined by the provided firing solution.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    public void TraverseTo(LosWeaponFiringSolution firingSolution) {
        D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        D.Assert(Weapon.IsOperational);
        D.AssertNull(_traverseJob, DebugName);

        //D.Log(ShowDebugLog, "{0} received Traverse to aim at {1}.", DebugName, firingSolution.EnemyTarget.DebugName);
        Quaternion reqdHubRotation = firingSolution.TurretRotation;
        Quaternion reqdBarrelElevation = firingSolution.TurretElevation;

        string jobName = "{0}.TraverseJob".Inject(DebugName);
        _traverseJob = _jobMgr.StartGameplayJob(ExecuteTraverse(reqdHubRotation, reqdBarrelElevation), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _traverseJob = null;
                HandleTraverseCompleted(firingSolution);
            }
        });
    }

    /// <summary>
    /// Coroutine that executes a traverse without overshooting.
    /// </summary>
    /// <param name="reqdHubRotation">The required rotation of the hub.</param>
    /// <param name="reqdBarrelElevation">The required (local) elevation of the barrel.</param>
    /// <returns></returns>
    private IEnumerator ExecuteTraverse(Quaternion reqdHubRotation, Quaternion reqdBarrelElevation) {
        Quaternion startingHubRotation = _hub.rotation;
        Quaternion startingBarrelElevation = _barrel.localRotation;
        float reqdHubRotationInDegrees = Quaternion.Angle(startingHubRotation, reqdHubRotation);
        float reqdBarrelElevationInDegrees = Quaternion.Angle(startingBarrelElevation, reqdBarrelElevation);
        //D.Log(ShowDebugLog, "Initiating {0} traversal. ReqdHubRotationInDegrees: {1:0.#}, ReqdBarrelElevationInDegrees: {2:0.#}.", DebugName, reqdHubRotationInDegrees, reqdBarrelElevationInDegrees);
        float hubActualDeviation;
        float barrelActualDeviation;

        bool isHubRotationCompleted = _hub.rotation.IsSame(reqdHubRotation, out hubActualDeviation, AllowedTraverseInaccuracy);
        bool isBarrelElevationCompleted = _barrel.localRotation.IsSame(reqdBarrelElevation, out barrelActualDeviation, AllowedTraverseInaccuracy);
        bool isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;
        float actualHubRotationInDegrees = 0F;
        float actualBarrelElevationInDegrees = 0F;
        float deltaTime;

        bool isInformedOfDateLogging = false;
        bool isInformedOfDateWarning = false;
        bool isInformedOfDateError = false;
        GameDate logDate = CalcLatestDateToCompleteTraverse();
        GameDate warnDate = default(GameDate);
        GameDate errorDate = default(GameDate);
        GameDate currentDate = _gameTime.CurrentDate;

        while (!isTraverseCompleted) {
            deltaTime = _gameTime.DeltaTime;
            if (!isHubRotationCompleted) {
                Quaternion previousHubRotation = _hub.rotation;
                float allowedHubRotationChange = HubRotationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                Quaternion inprocessRotation = Quaternion.RotateTowards(previousHubRotation, reqdHubRotation, allowedHubRotationChange);
                //float rotationChangeInDegrees = Quaternion.Angle(previousHubRotation, inprocessRotation);
                //D.Log(ShowDebugLog, "{0}: AllowedHubRotationChange = {1}, ActualHubRotationChange = {2}.", DebugName, allowedHubRotationChange, rotationChangeInDegrees);
                isHubRotationCompleted = inprocessRotation.IsSame(reqdHubRotation, out hubActualDeviation, AllowedTraverseInaccuracy);
                _hub.rotation = inprocessRotation;
            }

            if (!isBarrelElevationCompleted) {
                Quaternion previousBarrelElevation = _barrel.localRotation;
                float allowedBarrelElevationChange = BarrelElevationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                Quaternion inprocessElevation = Quaternion.RotateTowards(previousBarrelElevation, reqdBarrelElevation, allowedBarrelElevationChange);
                //float elevationChangeInDegrees = Quaternion.Angle(previousBarrelElevation, inprocessElevation);
                //D.Log(ShowDebugLog, "{0}: AllowedBarrelElevationChange = {1}, ActualBarrelElevationChange = {2}.", DebugName, allowedBarrelElevationChange, elevationChangeInDegrees);
                isBarrelElevationCompleted = inprocessElevation.IsSame(reqdBarrelElevation, out barrelActualDeviation, AllowedTraverseInaccuracy);
                _barrel.localRotation = inprocessElevation;
            }
            isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;

            if (!isTraverseCompleted && (currentDate = _gameTime.CurrentDate) > logDate) {
                if (!isInformedOfDateLogging) {
                    D.Log(ShowDebugLog, "{0}: CurrentDate {1} > LogDate {2} while traversing. HubActualDeviation = {3}, BarrelActualDeviation = {4}.",
                        DebugName, currentDate, logDate, hubActualDeviation, barrelActualDeviation);
                    isInformedOfDateLogging = true;
                }

                if (warnDate == default(GameDate)) {
                    warnDate = new GameDate(logDate, GameTimeDuration.OneDay);
                }
                if (currentDate > warnDate) {
                    if (!isInformedOfDateWarning) {
                        D.Warn("{0}: CurrentDate {1} > WarnDate {2} while traversing. HubActualDeviation = {3}, BarrelActualDeviation = {4}.",
                            DebugName, currentDate, warnDate, hubActualDeviation, barrelActualDeviation);
                        isInformedOfDateWarning = true;
                    }

                    if (errorDate == default(GameDate)) {
                        errorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
                    }
                    if (currentDate > errorDate) {
                        if (!isInformedOfDateError) {
                            if (!isHubRotationCompleted) {
                                actualHubRotationInDegrees = Quaternion.Angle(startingHubRotation, _hub.rotation);
                                D.Error("{0}.ExecuteTraverse timed out: ReqdHubRotation = {1}, ActualHubRotation = {2}, ActualDeviation = {3}, AllowedDeviation = {4:0.00}.",
                                    DebugName, reqdHubRotationInDegrees, actualHubRotationInDegrees, hubActualDeviation, AllowedTraverseInaccuracy);
                            }
                            if (!isBarrelElevationCompleted) {
                                actualBarrelElevationInDegrees = Quaternion.Angle(startingBarrelElevation, _barrel.localRotation);
                                D.Error("{0}.ExecuteTraverse timed out: ReqdBarrelElevation = {1}, ActualBarrelElevation = {2}, ActualDeviation = {3}, AllowedDeviation = {4:0.00}.",
                                    DebugName, reqdBarrelElevationInDegrees, actualBarrelElevationInDegrees, barrelActualDeviation, AllowedTraverseInaccuracy);
                            }
                            isInformedOfDateError = true;
                        }
                    }
                }

                if (ShowDebugLog) {
                    if (!isHubRotationCompleted) {
                        actualHubRotationInDegrees = Quaternion.Angle(startingHubRotation, _hub.rotation);
                        D.Log("{0}: ReqdHubRotation = {1}, ActualHubRotation = {2}, AllowedInaccuracy = {3:0.00}.", DebugName, reqdHubRotationInDegrees, actualHubRotationInDegrees, AllowedTraverseInaccuracy);
                    }
                    if (!isBarrelElevationCompleted) {
                        actualBarrelElevationInDegrees = Quaternion.Angle(startingBarrelElevation, _barrel.localRotation);
                        D.Log("{0}: ReqdBarrelElevation = {1}, ActualBarrelElevation = {2}, AllowedInaccuracy = {3:0.00}.", DebugName, reqdBarrelElevationInDegrees, actualBarrelElevationInDegrees, AllowedTraverseInaccuracy);
                    }
                }
            }
            yield return null;
        }
        //D.Log(ShowDebugLog, "{0} completed Traverse Job on {1}. Hub rotated {2:0.#} degrees, Barrel elevated {3:0.#} degrees.", DebugName, currentDate, reqdHubRotationInDegrees, reqdBarrelElevationInDegrees);
        //D.Warn(actualHubRotationInDegrees < Constants.ZeroF || actualHubRotationInDegrees > HubMaxRotationTraversal, "{0} completed Traverse Job with unexpected Hub Rotation change of {1:0.#} degrees.", DebugName, actualHubRotationInDegrees);
        //D.Warn(actualBarrelElevationInDegrees < Constants.ZeroF || actualBarrelElevationInDegrees > BarrelMaxElevationTraversal, "{0} completed Traverse Job with unexpected Barrel Elevation change of {1:0.#} degrees.", DebugName, actualBarrelElevationInDegrees);
    }

    /// <summary>
    /// Tests whether this turret can traverse to acquire the provided targetPosition. 
    /// Returns <c>true</c> if the turret can traverse far enough to bear on the targetPosition, <c>false</c> otherwise. 
    /// Returns the calculated hub rotation and barrel elevation values required for the turret to bear on the target, 
    /// even if the turret cannot traverse that far.
    /// <remarks>Uses rotated versions of the UpTurret prefab where the rotation is added to the turret object itself so that it appears
    /// to the hub like the ship is rotated.</remarks>
    /// </summary>
    /// <param name="targetPosition">The target position.</param>
    /// <param name="reqdHubRotation">The required hub rotation.</param>
    /// <param name="reqdBarrelElevation">The required barrel elevation.</param>
    /// <returns></returns>
    private bool TryCalcTraverse(Vector3 targetPosition, out Quaternion reqdHubRotation, out Quaternion reqdBarrelElevation) {
        // hub rotates within a plane defined by the contour of the hull it resides on. That plane is defined by the position of the hub and the hub's normal. 
        // This distance is to a parallel plane that contains the target.
        float signedDistanceToPlaneParallelToHubContainingTarget = Vector3.Dot(_hub.up, targetPosition - _hub.position);
        //D.Log(ShowDebugLog, "{0}: DistanceToPlaneParallelToHubContainingTarget = {1}.", DebugName, signedDistanceToPlaneParallelToHubContainingTarget);

        Vector3 targetPositionProjectedOntoHubPlane = targetPosition - _hub.up * signedDistanceToPlaneParallelToHubContainingTarget;
        //D.Log(ShowDebugLog, "{0}: TargetPositionProjectedOntoHubPlane = {1}.", DebugName, targetPositionProjectedOntoHubPlane);

        Vector3 vectorToTargetPositionProjectedOntoHubPlane = targetPositionProjectedOntoHubPlane - _hub.position;
        //D.Log(ShowDebugLog, "{0}: VectorToTargetPositionProjectedOntoHubPlane = {1}.", DebugName, vectorToTargetPositionProjectedOntoHubPlane);

        if (!vectorToTargetPositionProjectedOntoHubPlane.IsSameAs(Vector3.zero)) {
            // LookRotation throws an error if the vector to the target is zero, aka directly above the hub
            reqdHubRotation = Quaternion.LookRotation(vectorToTargetPositionProjectedOntoHubPlane, _hub.up);
        }
        else {
            // target is directly above turret so any rotation will work
            reqdHubRotation = _hub.rotation;
        }

        // assumes barrel local Z plane is same as hub plane which is true when the hub and barrels positions are the same, aka they pivot around the same point in space
        float barrelLocalZDistanceToTarget = vectorToTargetPositionProjectedOntoHubPlane.magnitude;

        // barrel always elevates around its local x Axis
        Vector3 localBarrelVectorToTarget = new Vector3(Constants.ZeroF, signedDistanceToPlaneParallelToHubContainingTarget, barrelLocalZDistanceToTarget);
        //D.Log(ShowDebugLog, "{0}: LocalBarrelVectorToTarget = {1}.", DebugName, localBarrelVectorToTarget);

        Vector3 vectorToTargetPositionProjectedOntoBarrelPlane = localBarrelVectorToTarget;    // simply for clarity

        if (!vectorToTargetPositionProjectedOntoBarrelPlane.IsSameAs(Vector3.zero)) {
            reqdBarrelElevation = Quaternion.LookRotation(vectorToTargetPositionProjectedOntoBarrelPlane);
            //D.Log(ShowDebugLog, "{0}: CalculatedBarrelElevationAngle = {1}.", DebugName, reqdBarrelElevation.eulerAngles);
        }
        else {
            // target is directly above/inFrontOf/below turret so return barrels to their amidships bearing to hit it?
            //D.Log(ShowDebugLog, "{0}: Target is directly in front of turret so barrels elevating to max.", DebugName);
            reqdBarrelElevation = _barrelMaxElevation;
        }

        var elevationAngleDeviationFromMax = Quaternion.Angle(reqdBarrelElevation, _barrelMaxElevation);
        //D.Log(ShowDebugLog, "{0}: ReqdBarrelElevationAngleDeviationFromMax = {1:0.#}, AllowedDeviationFromMax = {2:0.#}.", DebugName, elevationAngleDeviationFromMax, _allowedBarrelElevationAngleDeviationFromMax);

        return elevationAngleDeviationFromMax <= _allowedBarrelElevationAngleDeviationFromMax;
    }

    private void HandleTraverseCompleted(LosWeaponFiringSolution firingSolution) {
        Weapon.HandleWeaponAimed(firingSolution);
    }

    #region Event and Property Change Handlers

    private void WeaponIsOperationalChangedEventHandler(object sender, EventArgs e) {
        if (!Weapon.IsOperational) {
            KillTraverseJob();
        }
    }
    //private void WeaponIsOperationalChangedEventHandler(object sender, EventArgs e) {
    //    D.AssertNotNull(_traverseJob, "{0} received WeaponIsOperational change without traverse underway.".Inject(DebugName));
    //    if (Weapon.IsOperational) {
    //        D.Error("{0} should not become operational while traversing.", Weapon.DebugName);
    //    }
    //    // Neither Assert should fail as only subscribed to weapon.IsOperationalChanged event when we are traversing and weapon 
    //    // is already operational. 11.4.16 I saw this fail when AlertStatus changed from Red to Yellow and immediately back to Red 
    //    // which turned weapons off and immediately back on. As unsubscribing from the event was occurring on the job's jobCompleted
    //    // delegate, the time delay before executing jobCompleted created the window for this Assert to fail.
    //    //D.Log(ShowDebugLog, "{0}.IsOperational changed to {1} while traversing, killing TraverseJob. Weapon.IsDamaged = {2}.", Weapon.DebugName, Weapon.IsOperational, Weapon.IsDamaged);
    //    KillTraverseJob();
    //    // this unsubscribe action here avoids waiting an extra frame for _traverseJob's onCompleted delegate to fire
    //    Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
    //}

    protected override void WeaponPropSetHandler() {
        base.WeaponPropSetHandler();
        Weapon.isOperationalChanged += WeaponIsOperationalChangedEventHandler;
    }

    #endregion

    private void KillTraverseJob() {
        if (_traverseJob != null) {
            _traverseJob.Kill();
            _traverseJob = null;
        }
    }

    // 8.12.16 Job pausing moved to JobManager to consolidate handling

    private GameDate CalcLatestDateToCompleteTraverse() {
        var latestDateToCompleteHubRotation = DebugUtility.CalcWarningDateForRotation(HubRotationRate, HubMaxRotationTraversal);
        var latestDateToCompleteBarrelElevation = DebugUtility.CalcWarningDateForRotation(BarrelElevationRate, BarrelMaxElevationTraversal);
        return latestDateToCompleteHubRotation >= latestDateToCompleteBarrelElevation ? latestDateToCompleteHubRotation : latestDateToCompleteBarrelElevation;
    }

    protected override void Cleanup() {
        // 12.8.16 Job Disposal centralized in JobManager
        KillTraverseJob();
        Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    #endregion

}

