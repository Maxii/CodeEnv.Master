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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A Turret Weapon Mount for Line Of Sight Weapons like Beams and Projectiles.
/// </summary>
public class LOSTurret : AWeaponMount, ILOSWeaponMount {

    private static string _nameFormat = "{0}.{1}.{2}.{3}";

    private static LayerMask _defaultOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);

    /// <summary>
    /// The barrel's maximum elevation angle. 
    /// This value when its sign is inverted and applied to a Euler angle generates a rotation that points straight out from the turret.
    /// </summary>
    private static float _barrelMaxElevationAngle = 90F;

    /// <summary>
    /// The maximum elevation of the barrel of this turret which is the midpoint in its elevation traverse range.
    /// <remarks>Barrel x elevation angle to face outward must be negative due to the orientation of the local x axis of the barrel prefab.</remarks>
    /// </summary>
    private static Quaternion _barrelMaxElevation = Quaternion.Euler(new Vector3(-_barrelMaxElevationAngle, Constants.ZeroF, Constants.ZeroF));

    /// <summary>
    /// The rotation rate of the hub of this turret in degrees per hour.
    /// </summary>
    private static float _hubRotationRate = 270F;

    /// <summary>
    /// The elevation change rate of the barrel of this turret in degrees per hour.
    /// </summary>
    private static float _barrelElevationRate = 90F;

    /// <summary>
    /// The allowed deviation in degrees when determining when a traverse is completed.
    /// </summary>
    private static float _allowedTraversalDeviation = .01F;

    public Transform hub;

    public Transform barrel;

    public override string Name {
        get {
            if (Weapon == null) {
                return base.Name;
            }
            return _nameFormat.Inject(Weapon.Name, GetType().Name, facing.GetValueName(), SlotID.GetValueName());
        }
    }

    public override Vector3 MuzzleFacing { get { return (muzzle.position - barrel.position).normalized; } }

    public new ALOSWeapon Weapon {
        get { return base.Weapon as ALOSWeapon; }
        set { base.Weapon = value; }
    }

    /// <summary>
    /// The elevation the turret barrel rests at when not in use. 
    /// Currently initialized to facing forward in the case of turrets on top, bottom, port and starboard,
    /// and facing downward for any forward and aft turrets. Not currently used. // IMPROVE
    /// </summary>
    private Quaternion _barrelRestElevation;

    /// <summary>
    /// The allowed number of degrees the barrel elevation angle can deviate from maximum.
    /// Used to determine when the barrel is within its traversal range.
    /// </summary>
    private float _allowedBarrelElevationAngleDeviationFromMax;
    private GameTime _gameTime;
    private Job _traverseJob;

    #region Debug

    // Externalized this way to allow reporting of the results of a traverse.
    Vector3 __vectorToTargetPositionProjectedOntoHubPlane;
    Vector3 __vectorToTargetPositionProjectedOntoBarrelPlane;

    #endregion

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _gameTime = GameTime.Instance;
        _barrelRestElevation = barrel.localRotation;
    }

    protected override void Validate() {
        base.Validate();
        D.Assert(hub != null);
        D.Assert(barrel != null);
    }

    /// <summary>
    /// Initializes the barrel elevation settings based off of the provided minimum barrel elevation angle.
    /// </summary>
    /// <param name="minBarrelElevationAngle">The minimum barrel elevation angle.</param>
    public void InitializeBarrelElevationSettings(float minBarrelElevationAngle) {
        _allowedBarrelElevationAngleDeviationFromMax = _barrelMaxElevationAngle - minBarrelElevationAngle;
    }

    /// <summary>
    /// Trys to develop a firing solution from this WeaponMount to the provided target. If successful, returns <c>true</c> and provides the
    /// firing solution, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <param name="firingSolution"></param>
    /// <returns></returns>
    public override bool TryGetFiringSolution(IElementAttackableTarget enemyTarget, out FiringSolution firingSolution) {
        D.Assert(enemyTarget.IsOperational);
        D.Assert(enemyTarget.Owner.IsEnemyOf(Weapon.Owner));

        firingSolution = null;
        Vector3 targetPosition = enemyTarget.Position;
        Vector3 vectorToTarget = targetPosition - hub.position;
        //D.Log("{0}: TargetBearing = {1}.", Name, vectorToTarget.normalized);
        float targetDistance = vectorToTarget.magnitude;
        if (targetDistance > Weapon.RangeDistance) {
            D.Log("{0}: Target {1} is out of range.", Name, enemyTarget.FullName);
            return false;
        }

        Quaternion reqdHubRotation, reqdBarrelElevation;
        bool canTraverseToTarget = TryCalcTraverse(targetPosition, out reqdHubRotation, out reqdBarrelElevation);
        if (!canTraverseToTarget) {
            D.Log("{0}: Target {1} is beyond traverse range.", Name, enemyTarget.FullName);
            return false;
        }

        bool isLosClear = CheckLineOfSight(enemyTarget);
        if (!isLosClear) {
            return false;
        }
        firingSolution = new LosFiringSolution(Weapon, enemyTarget, reqdHubRotation, reqdBarrelElevation);
        return true;
    }

    /// <summary>
    /// Checks the line of sight from this LOSWeaponMount to the provided enemy target, returning <c>true</c>
    /// if there is a clear line of sight to the target, otherwise <c>false</c>.
    /// </summary>
    /// <param name="enemyTarget">The enemy target.</param>
    /// <returns></returns>
    private bool CheckLineOfSight(IElementAttackableTarget enemyTarget) {
        Vector3 vectorToTarget = enemyTarget.Position - hub.position;
        Vector3 targetDirection = vectorToTarget.normalized;
        float targetDistance = vectorToTarget.magnitude;
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, targetDirection, out hitInfo, targetDistance, _defaultOnlyLayerMask)) {
            var targetHit = hitInfo.transform.gameObject.GetSafeInterface<IElementAttackableTarget>();
            if (targetHit != null) {
                if (targetHit == enemyTarget) {
                    //D.Log("{0}: CheckLineOfSight({1}) found its target.", Name, enemyTarget.FullName);
                    return true;
                }
                if (targetHit.Owner.IsEnemyOf(Weapon.Owner)) {
                    D.Log("{0}: CheckLineOfSight({1}) found interfering enemy target {2}.", Name, enemyTarget.FullName, targetHit.FullName);
                    return false;
                }
                D.Log("{0}: CheckLineOfSight({1}) found interfering non-enemy target {2}.", Name, enemyTarget.FullName, targetHit.FullName);
                return false;
            }
            D.Warn("{0}: CheckLineOfSight() didn't find target {1} but found {2}.", Name, enemyTarget.FullName, hitInfo.transform.name);
            return false;
        }
        D.Warn("{0}: CheckLineOfSight({1}) didn't find anything.", Name, enemyTarget.FullName);    // shouldn't happen?
        return false;
    }

    /// <summary>
    /// Traverses the mount to point at the target defined by the provided firing solution.
    /// </summary>
    /// <param name="firingSolution"></param>
    public void TraverseTo(LosFiringSolution firingSolution) {
        Traverse(firingSolution, allowedTime: 5F);
    }

    /// <summary>
    /// Traverses the mount to point at the target defined by the provided firing solution.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    /// <param name="allowedTime">The allowed time before an error is thrown.</param>
    private void Traverse(LosFiringSolution firingSolution, float allowedTime = 20F) {
        IElementAttackableTarget target = firingSolution.EnemyTarget;
        string targetName = target.FullName;
        D.Log("{0} received Traverse to aim at {1}.", Name, targetName);

        Quaternion reqdHubRotation = firingSolution.TurretRotation;
        Quaternion reqdBarrelElevation = firingSolution.TurretElevation;

        if (_traverseJob != null && _traverseJob.IsRunning) {
            _traverseJob.Kill();
            // onJobComplete will run next frame so placed cancelled notice here
            D.Error("{0}'s previous Traverse Job has been cancelled.", Name);   // if this happens, no onTraverseCompleted will be returned for cancelled traverse
        }

        _traverseJob = new Job(ExecuteTraverse(reqdHubRotation, reqdBarrelElevation, allowedTime), toStart: true, onJobComplete: (jobWasKilled) => {
            if (!jobWasKilled) {
                D.Log("{0}'s traverse to aim at {1} complete.", Name, targetName);
                Vector3 actualTargetBearing = (target.Position - barrel.position).normalized;
                float deviationAngle = Vector3.Angle(MuzzleFacing, actualTargetBearing);
                //D.Log("{0}: HubFacingAfterRotation Intended = {1}, Actual = {2}.", Name, __vectorToTargetPositionProjectedOntoHubPlane.normalized, hub.forward);
                //Vector3 barrelLocalFacing = hub.InverseTransformDirection(barrel.forward);
                //D.Log("{0}: LocalBarrelFacingAfterRotation Intended = {1}, Actual = {2}.", Name, __vectorToTargetPositionProjectedOntoBarrelPlane.normalized, barrelLocalFacing);
                D.Log("{0}: DeviationAngle = {1}, ActualTargetBearing = {2}, MuzzleBearingAfterTraverse = {3}.", Name, deviationAngle, actualTargetBearing, MuzzleFacing);
                OnTraverseCompleted(firingSolution);
            }
        });
    }

    /// <summary>
    /// Coroutine that executes a traverse without overshooting.
    /// </summary>
    /// <param name="reqdHubRotation">The required rotation of the hub.</param>
    /// <param name="reqdBarrelElevation">The required (local) elevation of the barrel.</param>
    /// <param name="allowedTime">The allowed time in GameTimeSeconds.</param>
    /// <returns></returns>
    private IEnumerator ExecuteTraverse(Quaternion reqdHubRotation, Quaternion reqdBarrelElevation, float allowedTime) {
        //D.Log("Initiating {0} traversal. HubRotationRate: {1:0.}, BarrelElevationRate: {2:0.} degrees/hour.", Name, _hubRotationRate, _barrelElevationRate);
        float hubRotationRateInDegreesPerSecond = _hubRotationRate * GameTime.HoursPerSecond;
        float barrelElevationRateInDegreesPerSecond = _barrelElevationRate * GameTime.HoursPerSecond;
        float cumTime = Constants.ZeroF;
        bool isHubRotationCompleted = hub.rotation.IsSame(reqdHubRotation, _allowedTraversalDeviation);
        bool isBarrelElevationCompleted = barrel.localRotation.IsSame(reqdBarrelElevation, _allowedTraversalDeviation);
        bool isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;
        while (!isTraverseCompleted) {
            float deltaTime = _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            if (!isHubRotationCompleted) {
                //Quaternion previousHubRotation = hub.rotation;
                float allowedHubRotationChange = hubRotationRateInDegreesPerSecond * deltaTime;
                hub.rotation = Quaternion.RotateTowards(hub.rotation, reqdHubRotation, allowedHubRotationChange);
                //float rotationChangeInDegrees = Quaternion.Angle(previousHubRotation, hub.rotation);
                //D.Log("{0}: AllowedHabRotationChange = {1}, ActualHabRotationChange = {2}.", Name, allowedHabRotationChange, rotationChangeInDegrees);
                isHubRotationCompleted = hub.rotation.IsSame(reqdHubRotation, _allowedTraversalDeviation);
            }

            if (!isBarrelElevationCompleted) {
                float allowedBarrelElevationChange = barrelElevationRateInDegreesPerSecond * deltaTime;
                barrel.localRotation = Quaternion.RotateTowards(barrel.localRotation, reqdBarrelElevation, allowedBarrelElevationChange);
                isBarrelElevationCompleted = barrel.localRotation.IsSame(reqdBarrelElevation, _allowedTraversalDeviation);
            }
            isTraverseCompleted = isHubRotationCompleted && isBarrelElevationCompleted;

            cumTime += _gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
            D.Assert(cumTime < allowedTime, "{0}: CumTime {1} > AllowedTime {2}.".Inject(Name, cumTime, allowedTime));
            yield return null; // Note: see Navigator.ExecuteHeadingChange() if wish to use WaitForSeconds() or WaitForFixedUpdate()
        }
        //D.Log("{0} completed Traverse Job. Duration = {1:0.####} GameTimeSecs.", Name, cumTime);
    }

    /// <summary>
    /// Tests whether this turret can traverse to acquire the provided targetPosition. Returns <c>true</c> if the turret can traverse far enough to
    /// bear on the targetPosition, <c>false</c> otherwise. Returns the calculated hub and barrel rotation
    /// and elevation values required for the turret to bear on the target, even if the turret cannot traverse that far.
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
        float signedDistanceToPlaneParallelToHubContainingTarget = Vector3.Dot(hub.up, targetPosition - hub.position);
        //D.Log("{0}: DistanceToPlaneParallelToHubContainingTarget = {1}.", Name, signedDistanceToPlaneParallelToHubContainingTarget);

        Vector3 targetPositionProjectedOntoHubPlane = targetPosition - hub.up * signedDistanceToPlaneParallelToHubContainingTarget;
        //D.Log("{0}: TargetPositionProjectedOntoHubPlane = {1}.", Name, targetPositionProjectedOntoHubPlane);

        __vectorToTargetPositionProjectedOntoHubPlane = targetPositionProjectedOntoHubPlane - hub.position;
        //D.Log("{0}: VectorToTargetPositionProjectedOntoHubPlane = {1}.", Name, __vectorToTargetPositionProjectedOntoHubPlane);

        if (!__vectorToTargetPositionProjectedOntoHubPlane.IsSameAs(Vector3.zero)) {
            // LookRotation throws an error if the vector to the target is zero, aka directly above the hub
            reqdHubRotation = Quaternion.LookRotation(__vectorToTargetPositionProjectedOntoHubPlane, hub.up);
        }
        else {
            // target is directly above turret so any rotation will work
            reqdHubRotation = hub.rotation;
        }

        // assumes barrel local Z plane is same as hub plane which is true when the hub and barrels positions are the same, aka they pivot around the same point in space
        float barrelLocalZDistanceToTarget = __vectorToTargetPositionProjectedOntoHubPlane.magnitude;

        // barrel always elevates around its local x Axis
        Vector3 localBarrelVectorToTarget = new Vector3(Constants.ZeroF, signedDistanceToPlaneParallelToHubContainingTarget, barrelLocalZDistanceToTarget);
        //D.Log("{0}: LocalBarrelVectorToTarget = {1}.", Name, localBarrelVectorToTarget);

        __vectorToTargetPositionProjectedOntoBarrelPlane = localBarrelVectorToTarget;    // simply for clarity

        if (!__vectorToTargetPositionProjectedOntoBarrelPlane.IsSameAs(Vector3.zero)) {
            reqdBarrelElevation = Quaternion.LookRotation(__vectorToTargetPositionProjectedOntoBarrelPlane);
            //D.Log("{0}: CalculatedBarrelElevationAngle = {1}.", Name, reqdBarrelElevation.eulerAngles);
        }
        else {
            // target is directly above/infrontof/below turret so return barrels to their amidships bearing to hit it?
            D.Log("{0}: Target is directly in front of turret so barrels elevating to max.", Name);
            reqdBarrelElevation = _barrelMaxElevation;
        }

        var elevationAngleDeviationFromMax = Quaternion.Angle(reqdBarrelElevation, _barrelMaxElevation);
        //D.Log("{0}: ReqdBarrelElevationAngleDeviationFromMax = {1:0.#}, AllowedDeviationFromMax = {2:0.#}.", Name, elevationAngleDeviationFromMax, _allowedBarrelElevationAngleDeviationFromMax);

        return elevationAngleDeviationFromMax <= _allowedBarrelElevationAngleDeviationFromMax;
    }

    private void OnTraverseCompleted(LosFiringSolution firingSolution) {
        Weapon.OnTraverseCompleted(firingSolution);
    }

    protected override void Cleanup() {
        if (_traverseJob != null && _traverseJob.IsRunning) {
            _traverseJob.Kill();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

