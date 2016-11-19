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
using UnityEngine.Serialization;

/// <summary>
/// A Turret Weapon Mount for Line Of Sight Weapons like Beams and Projectiles.
/// </summary>
public class LOSTurret : AWeaponMount, ILOSWeaponMount {

    private const string NameFormat = "{0}.{1}.{2}";

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

    public override string Name {
        get {
            if (Weapon == null) {
                return base.Name;
            }
            return NameFormat.Inject(Weapon.Name, GetType().Name, SlotID.GetValueName());
        }
    }

    public override Vector3 MuzzleFacing { get { return (Muzzle.position - _barrel.position).normalized; } }

    public new ALOSWeapon Weapon {
        get { return base.Weapon as ALOSWeapon; }
        set { base.Weapon = value; }
    }

    /// <summary>
    /// The inaccuracy of this Turret when traversing in degrees.
    /// Affects both hub rotation and barrel elevation.
    /// </summary>
    private float AllowedTraverseInaccuracy { get { return UnityConstants.AngleEqualityPrecision; } }

    private bool IsTraverseJobRunning { get { return _traverseJob != null && _traverseJob.IsRunning; } }

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
        D.Assert(enemyTarget.IsAttackByAllowed(Weapon.Owner));

        firingSolution = null;
        if (!ConfirmInRangeForLaunch(enemyTarget)) {
            //D.Log("{0}: Target {1} is out of range.", Name, enemyTarget.FullName);
            return false;
        }

        Vector3 targetPosition = enemyTarget.Position;
        Quaternion reqdHubRotation, reqdBarrelElevation;
        bool canTraverseToTarget = TryCalcTraverse(targetPosition, out reqdHubRotation, out reqdBarrelElevation);
        if (!canTraverseToTarget) {
            //D.Log("{0}: Target {1} is out of traverse range.", Name, enemyTarget.FullName);
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

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var attackableTgtEncountered = raycastHitInfo.transform.GetComponent<IElementAttackable>();
            Profiler.EndSample();

            if (attackableTgtEncountered != null) {
                if (attackableTgtEncountered == enemyTarget) {
                    //D.Log("{0}: CheckLineOfSight({1}) found its target.", Name, enemyTarget.FullName);
                    return true;
                }
                if (attackableTgtEncountered.IsAttackByAllowed(Weapon.Owner)) {
                    D.Log("{0}: CheckLineOfSight({1}) found interfering attackable target {2} on {3}.", Name, enemyTarget.FullName, attackableTgtEncountered.FullName, _gameTime.CurrentDate);
                    return false;
                }
                D.Log("{0}: CheckLineOfSight({1}) found interfering non-attackable target {2} on {3}.", Name, enemyTarget.FullName, attackableTgtEncountered.FullName, _gameTime.CurrentDate);
                return false;
            }
            D.Log("{0}: CheckLineOfSight({1}) didn't find target but found {2} on {3}.", Name, enemyTarget.FullName, raycastHitInfo.transform.name, _gameTime.CurrentDate);
            return false;
        }
        //D.Log("{0}: CheckLineOfSight({1}) didn't find anything. Date: {2}.", Name, enemyTarget.FullName, _gameTime.CurrentDate);
        return true;
    }

    /// <summary>
    /// Traverses the mount to point at the target defined by the provided firing solution.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    public void TraverseTo(LosWeaponFiringSolution firingSolution) {
        //D.Log("{0} received Traverse to aim at {1}.", Name, firingSolution.EnemyTarget.FullName);
        D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");

        D.Assert(Weapon.IsOperational);
        Weapon.isOperationalChanged += WeaponIsOperationalChangedEventHandler;  // 3.29.16 added operational check as weapon can be damaged while traversing

        Quaternion reqdHubRotation = firingSolution.TurretRotation;
        Quaternion reqdBarrelElevation = firingSolution.TurretElevation;

        if (IsTraverseJobRunning) {
            D.Error("{0} received TraverseTo while traversing to {1}.", Name, __lastFiringSolution);
        }
        var errorDate = CalcLatestDateToCompleteTraverse();
        //D.Log("{0}: time allowed to Traverse = {1}.", Name, errorDate - _gameTime.CurrentDate);
        string jobName = "{0}.TraverseJob".Inject(Name);
        _traverseJob = _jobMgr.StartGameplayJob(ExecuteTraverse(reqdHubRotation, reqdBarrelElevation, errorDate), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
            // 11.4.16 Moved unsubscribing from Weapon.isOperationalChanged to last line of ExecuteTraverse() to handle normal Job completion,
            // and to KillTraverseJob() when the job needs to be killed. This eliminates the one frame delay that comes with using the 
            // jobCompleted delegate which caused problems when a weapon was activated immediately after being deactivated. The immediate 
            // activation after deactivation occurred before jobCompleted could unsubscribe, causing the Assert in 
            // WeaponIsOperationalChangedEventHandler() to fail. I eliminated the deactivation/activation sequence (caused by back to back 
            // AlertStatus changes) but concluded adopting the above pattern was good practice. See Note 1 in Job class.
            if (!jobWasKilled) {
                HandleTraverseCompleted(firingSolution);
            }
        });
        __lastFiringSolution = firingSolution;
    }

    /// <summary>
    /// Coroutine that executes a traverse without overshooting.
    /// </summary>
    /// <param name="reqdHubRotation">The required rotation of the hub.</param>
    /// <param name="reqdBarrelElevation">The required (local) elevation of the barrel.</param>
    /// <param name="errorDate">The date after which an error is thrown.</param>
    /// <returns></returns>
    private IEnumerator ExecuteTraverse(Quaternion reqdHubRotation, Quaternion reqdBarrelElevation, GameDate errorDate) {
        //Quaternion startingHubRotation = _hub.rotation;
        //Quaternion startingBarrelElevation = _barrel.localRotation;
        //float reqdHubRotationInDegrees = Quaternion.Angle(startingHubRotation, reqdHubRotation);
        //float reqdBarrelElevationInDegrees = Quaternion.Angle(startingBarrelElevation, reqdBarrelElevation);
        //D.Log("Initiating {0} traversal. ReqdHubRotationInDegrees: {1:0.#}, ReqdBarrelElevationInDegrees: {2:0.#}.", Name, reqdHubRotationInDegrees, reqdBarrelElevationInDegrees);
        bool isHubRotationCompleted = _hub.rotation.IsSame(reqdHubRotation, AllowedTraverseInaccuracy);
        bool isBarrelElevationCompleted = _barrel.localRotation.IsSame(reqdBarrelElevation, AllowedTraverseInaccuracy);
        bool isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;
        //float actualHubRotationInDegrees = 0F;
        //float actualBarrelElevationInDegrees = 0F;
        float deltaTime;
        GameDate currentDate = _gameTime.CurrentDate;
        while (!isTraverseCompleted) {
            deltaTime = _gameTime.DeltaTime;
            if (!isHubRotationCompleted) {
                Quaternion previousHubRotation = _hub.rotation;
                float allowedHubRotationChange = HubRotationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                Quaternion inprocessRotation = Quaternion.RotateTowards(previousHubRotation, reqdHubRotation, allowedHubRotationChange);
                //float rotationChangeInDegrees = Quaternion.Angle(previousHubRotation, inprocessRotation);
                //D.Log("{0}: AllowedHubRotationChange = {1}, ActualHubRotationChange = {2}.", Name, allowedHubRotationChange, rotationChangeInDegrees);
                isHubRotationCompleted = inprocessRotation.IsSame(reqdHubRotation, AllowedTraverseInaccuracy);
                _hub.rotation = inprocessRotation;
            }

            if (!isBarrelElevationCompleted) {
                Quaternion previousBarrelElevation = _barrel.localRotation;
                float allowedBarrelElevationChange = BarrelElevationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                Quaternion inprocessElevation = Quaternion.RotateTowards(previousBarrelElevation, reqdBarrelElevation, allowedBarrelElevationChange);
                //float elevationChangeInDegrees = Quaternion.Angle(previousBarrelElevation, inprocessElevation);
                //D.Log("{0}: AllowedBarrelElevationChange = {1}, ActualBarrelElevationChange = {2}.", Name, allowedBarrelElevationChange, elevationChangeInDegrees);
                isBarrelElevationCompleted = inprocessElevation.IsSame(reqdBarrelElevation, AllowedTraverseInaccuracy);
                _barrel.localRotation = inprocessElevation;
            }
            isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;

            //actualHubRotationInDegrees = Quaternion.Angle(startingHubRotation, _hub.rotation);
            //actualBarrelElevationInDegrees = Quaternion.Angle(startingBarrelElevation, _barrel.localRotation);

            if (!isTraverseCompleted) {
                //D.Assert(_gameTime.CurrentDate <= errorDate, "{0}: Exceeded error date {1} while traversing.", Name, errorDate);
                currentDate = _gameTime.CurrentDate;
                if (currentDate > errorDate) {
                    D.Warn("{0}: CurrentDate {1} > ErrorDate {2} while traversing.", Name, currentDate, errorDate);
                    //D.Warn(!isHubRotationCompleted, "{0}: ReqdHubRotation = {1}, ActualHubRotation = {2}, Inaccuracy = {3:0.00}.", Name, reqdHubRotationInDegrees, actualHubRotationInDegrees, TraverseInaccuracy);
                    //D.Warn(!isBarrelElevationCompleted, "{0}: ReqdBarrelElevation = {1}, ActualBarrelElevation = {2}, Inaccuracy = {3:0.00}.", Name, reqdBarrelElevationInDegrees, actualBarrelElevationInDegrees, TraverseInaccuracy);
                }
            }
            yield return null; // Note: see Navigator.ExecuteHeadingChange() if wish to use WaitForSeconds() or WaitForFixedUpdate()
        }
        Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
        //D.Log("{0} completed Traverse Job on {1}. Hub rotated {2:0.#} degrees, Barrel elevated {3:0.#} degrees.", Name, currentDate, reqdHubRotationInDegrees, reqdBarrelElevationInDegrees);
        //D.Warn(actualHubRotationInDegrees < Constants.ZeroF || actualHubRotationInDegrees > HubMaxRotationTraversal, "{0} completed Traverse Job with unexpected Hub Rotation change of {1:0.#} degrees.", Name, actualHubRotationInDegrees);
        //D.Warn(actualBarrelElevationInDegrees < Constants.ZeroF || actualBarrelElevationInDegrees > BarrelMaxElevationTraversal, "{0} completed Traverse Job with unexpected Barrel Elevation change of {1:0.#} degrees.", Name, actualBarrelElevationInDegrees);
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
        //D.Log("{0}: DistanceToPlaneParallelToHubContainingTarget = {1}.", Name, signedDistanceToPlaneParallelToHubContainingTarget);

        Vector3 targetPositionProjectedOntoHubPlane = targetPosition - _hub.up * signedDistanceToPlaneParallelToHubContainingTarget;
        //D.Log("{0}: TargetPositionProjectedOntoHubPlane = {1}.", Name, targetPositionProjectedOntoHubPlane);

        Vector3 vectorToTargetPositionProjectedOntoHubPlane = targetPositionProjectedOntoHubPlane - _hub.position;
        //D.Log("{0}: VectorToTargetPositionProjectedOntoHubPlane = {1}.", Name, vectorToTargetPositionProjectedOntoHubPlane);

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
        //D.Log("{0}: LocalBarrelVectorToTarget = {1}.", Name, localBarrelVectorToTarget);

        Vector3 vectorToTargetPositionProjectedOntoBarrelPlane = localBarrelVectorToTarget;    // simply for clarity

        if (!vectorToTargetPositionProjectedOntoBarrelPlane.IsSameAs(Vector3.zero)) {
            reqdBarrelElevation = Quaternion.LookRotation(vectorToTargetPositionProjectedOntoBarrelPlane);
            //D.Log("{0}: CalculatedBarrelElevationAngle = {1}.", Name, reqdBarrelElevation.eulerAngles);
        }
        else {
            // target is directly above/inFrontOf/below turret so return barrels to their amidships bearing to hit it?
            //D.Log("{0}: Target is directly in front of turret so barrels elevating to max.", Name);
            reqdBarrelElevation = _barrelMaxElevation;
        }

        var elevationAngleDeviationFromMax = Quaternion.Angle(reqdBarrelElevation, _barrelMaxElevation);
        //D.Log("{0}: ReqdBarrelElevationAngleDeviationFromMax = {1:0.#}, AllowedDeviationFromMax = {2:0.#}.", Name, elevationAngleDeviationFromMax, _allowedBarrelElevationAngleDeviationFromMax);

        return elevationAngleDeviationFromMax <= _allowedBarrelElevationAngleDeviationFromMax;
    }

    private void HandleTraverseCompleted(LosWeaponFiringSolution firingSolution) {
        Weapon.HandleWeaponAimed(firingSolution);
    }

    #region Event and Property Change Handlers

    private void WeaponIsOperationalChangedEventHandler(object sender, EventArgs e) {
        if (!IsTraverseJobRunning) {
            D.Error("{0} received WeaponIsOperational change without traverse underway.", Name);
        }
        if (Weapon.IsOperational) {
            D.Error("{0} should not become operational while traversing.", Weapon.FullName);
        }
        // Neither Assert should fail as only subscribed to weapon.IsOperationalChanged event when we are traversing and weapon 
        // is already operational. 11.4.16 I saw this fail when AlertStatus changed from Red to Yellow and immediately back to Red 
        // which turned weapons off and immediately back on. As unsubscribing from the event was occurring on the job's jobCompleted
        // delegate, the time delay before executing jobCompleted created the window for this Assert to fail.
        //D.Log("{0}.IsOperational changed to {1} while traversing, killing TraverseJob. Weapon.IsDamaged = {2}.", Weapon.FullName, Weapon.IsOperational, Weapon.IsDamaged);
        KillTraverseJob();
    }

    #endregion

    private void KillTraverseJob() {
        D.Assert(IsTraverseJobRunning);
        _traverseJob.Kill();
        Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
    }

    // 8.12.16 Job pausing moved to JobManager to consolidate handling

    private GameDate CalcLatestDateToCompleteTraverse() {
        var latestDateToCompleteHubRotation = GameUtility.CalcWarningDateForRotation(HubRotationRate, HubMaxRotationTraversal);
        var latestDateToCompleteBarrelElevation = GameUtility.CalcWarningDateForRotation(BarrelElevationRate, BarrelMaxElevationTraversal);
        return latestDateToCompleteHubRotation >= latestDateToCompleteBarrelElevation ? latestDateToCompleteHubRotation : latestDateToCompleteBarrelElevation;
    }

    protected override void Cleanup() {
        if (_traverseJob != null) {
            _traverseJob.Dispose();
        }
        Weapon.isOperationalChanged -= WeaponIsOperationalChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    /// <summary>
    /// Used for debugging when a traverse job is killed.
    /// </summary>
    private LosWeaponFiringSolution __lastFiringSolution;

    #endregion

}

