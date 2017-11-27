// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PhysicsProjectile.cs
// Unguided APhysicsProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Unguided APhysicsProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.  
/// </summary>
public class PhysicsProjectile : APhysicsProjectileOrdnance {

    [SerializeField]
    private GameObject _muzzleEffect = null;

    /// <summary>
    /// The effect this Projectile will show while operating including when the game is paused.
    /// </summary>
    [SerializeField]
    private ParticleSystem _operatingEffect = null;

    [SerializeField]
    private ParticleSystem _impactEffect = null;

    /// <summary>
    /// The maximum speed of this projectile in units per hour in Topography.OpenSpace.
    /// The actual speed of this projectile will be at this MaxSpeed when first fired. As it travels
    /// its speed will decline as the projectile's drag affects it. The projectile's drag will be greater
    /// in higher density Topography causing the projectile's actual speed to decline faster.
    /// </summary>
    public override float MaxSpeed {
        get { return _maxSpeed > Constants.ZeroF ? _maxSpeed : Weapon.MaxSpeed; }
    }

    /// <summary>
    /// The drag of this projectile in Topography.OpenSpace.
    /// </summary>
    public override float OpenSpaceDrag { get { return Weapon.OrdnanceDrag; } }

    public override float Mass { get { return Weapon.OrdnanceMass; } }

    protected new ProjectileLauncher Weapon { get { return base.Weapon as ProjectileLauncher; } }

    private Job _impactEffectCompletionJob;
    private Job _muzzleEffectCompletionJob;

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        AdjustHeadingForInaccuracy();
        InitializeVelocity();
        D.Assert(!enabled);
        enabled = true;
        _collider.enabled = true;
    }

    protected override void ValidateEffects() {
        D.AssertNotNull(_muzzleEffect);
        D.Assert(!_muzzleEffect.activeSelf, _muzzleEffect.name);
        if (_operatingEffect != null) {
            // ParticleSystem Operating Effect can be null. If so, it will be replaced by an Icon
            var operatingEffectMainModule = _operatingEffect.main;
            D.Assert(!operatingEffectMainModule.playOnAwake); //D.Assert(!_operatingEffect.playOnAwake); Deprecated in Unity 5.5
            D.Assert(operatingEffectMainModule.loop);   //D.Assert(_operatingEffect.loop); Deprecated in Unity 5.5
        }
        D.AssertNotNull(_impactEffect);
        var impactEffectMainModule = _impactEffect.main;
        // Awake only called once during GameObject life -> can't use with pooling
        D.Assert(!impactEffectMainModule.playOnAwake); //D.Assert(!_impactEffect.playOnAwake); Deprecated in Unity 5.5
        D.Assert(_impactEffect.gameObject.activeSelf, _impactEffect.name);
    }

    protected override AProjectileDisplayManager MakeDisplayMgr() {
        return new ProjectileDisplayManager(this, Layers.Cull_15, _operatingEffect);
    }

    /// <summary>
    /// Adjusts this projectile's heading for inaccuracy.
    /// <see cref="http://answers.unity3d.com/questions/887852/help-with-gun-accuracy-in-degrees.html"/>
    /// </summary>
    private void AdjustHeadingForInaccuracy() {
        Vector2 error = UnityEngine.Random.insideUnitCircle * Weapon.MaxLaunchInaccuracy;
        Quaternion errorRotation = Quaternion.Euler(error.x, error.y, Constants.ZeroF);
        Quaternion finalRotation = transform.rotation * errorRotation;
        if (ShowDebugLog) {
            Quaternion accurateRotation = transform.rotation;
            float errorAngle = Quaternion.Angle(accurateRotation, finalRotation);
            D.Log("{0} has incorporated {1:0.0} degrees of inaccuracy into its trajectory.", DebugName, errorAngle);
        }
        transform.rotation = finalRotation;
    }

    /// <summary>
    /// One-time initialization of the velocity of this 'projectile'.
    /// </summary>
    private void InitializeVelocity() {
        _rigidbody.velocity = CurrentHeading * MaxSpeed * _gameTime.GameSpeedAdjustedHoursPerSecond;
    }

    protected override void ShowMuzzleEffect() {
        D.AssertNull(_muzzleEffectCompletionJob);
        // relocate this Effect so it doesn't move with the projectile while showing
        UnityUtility.AttachChildToParent(_muzzleEffect, DynamicObjectsFolder.Instance.gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;
        _muzzleEffect.transform.position = Position;
        _muzzleEffect.transform.rotation = transform.rotation;
        _muzzleEffect.SetActive(true);
        string jobName = "{0}.WaitForMuzzleEffectCompletionJob".Inject(DebugName);
        _muzzleEffectCompletionJob = _jobMgr.WaitForGameplaySeconds(0.2F, jobName, waitFinished: (jobWasKilled) => {
            _muzzleEffect.SetActive(false);
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _muzzleEffectCompletionJob = null;
            }
        });
        //TODO Add audio
    }

    // OPTIMIZE particle system should be at correct scale to begin with so no runtime scaling reqd
    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        base.ShowImpactEffects(position, rotation);
        D.Assert(!_impactEffect.isPlaying); // should not be called more than once
        D.AssertNull(_impactEffectCompletionJob);   // should not be called more than once
        D.Assert(IsOperational);
        __ReduceScaleOfImpactEffect();
        _impactEffect.transform.position = position;
        _impactEffect.transform.rotation = rotation;
        _impactEffect.Play();
        bool includeChildren = true;
        string jobName = "{0}.WaitForImpactEffectCompletionJob".Inject(DebugName);   // pausable for debug observation
        _impactEffectCompletionJob = _jobMgr.WaitForParticleSystemCompletion(_impactEffect, includeChildren, jobName, isPausable: true, waitFinished: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _impactEffectCompletionJob = null;
                if (IsOperational) {    // UNCLEAR needed now that this is only reached when naturally completing?
                    // Ordnance has not already been terminated by other paths such as the death of the target
                    TerminateNow();
                }
            }
        });
    }

    protected override void HandleImpactEffectsBegun() {
        base.HandleImpactEffectsBegun();
        // nothing unique to shutdown 
    }

    protected override void HearImpactEffect(Vector3 position) {
        GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
        SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion    // FIXME ??
    }

    protected override void HandleCollision(GameObject impactedGo, ContactPoint contactPoint) {
        if (!IsOperational) {
            // 1.6.17 OnCollisionEnter is called even when both _collider and monoBehaviour are disabled according to OnCollisionEnter 
            // docs: "Collision events will be sent to disabled MonoBehaviours, to allow enabling Behaviours in response to collisions." 
            return;
        }
        //D.Log(ShowDebugLog, "{0} distance to intended target on collision: {1}.", DebugName, Vector3.Distance(transform.position, Target.Position));
        bool isImpactEffectRunning = false;

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var impactedTarget = impactedGo.GetComponent<IElementAttackable>();
        Profiler.EndSample();

        if (impactedTarget != null) {
            // hit an attackableTarget
            if (impactedTarget == Target) {
                D.Log(ShowDebugLog, "{0} collided with its Target {1}.", DebugName, Target.DebugName);
            }
            else {
                if (Target.IsOperational) {
                    D.Log(ShowDebugLog, "{0} collided with {1} rather than its target. TgtDistance = {2:0.#}, TgtBearingError = {3:0.#}.",
                        DebugName, impactedTarget.DebugName, Vector3.Distance(transform.position, Target.Position),
                        Vector3.Angle(CurrentHeading, Target.Position - Position));
                }
                else {
                    D.Log(ShowDebugLog, "{0} collided with {1} rather than its dead target {2}.", DebugName, impactedTarget.DebugName, Target.DebugName);
                }
            }
            // The application of impact force is already handled by the physics engine when regular rigidbodies collide
            if (impactedTarget.IsOperational) {
                if (impactedTarget == Target) {
                    ReportTargetHit();
                }
                else {
                    ReportInterdiction();   // Not from ActiveCM as they don't collide
                }
                impactedTarget.TakeHit(DamagePotential);
                // if target is killed by this hit, TerminateNow will be immediately called by Weapon
            }
            if (impactedTarget.IsOperational) {
                // target survived the hit
                if (impactedTarget.IsVisualDetailDiscernibleToUser) {
                    // target is being viewed by user so show impact effect
                    //D.Log(ShowDebugLog, "{0} starting impact effect on {1}.", DebugName, impactedTarget.DisplayName);
                    var impactEffectLocation = contactPoint.point + contactPoint.normal * 0.01F;    // HACK
                    // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                    ShowImpactEffects(impactEffectLocation);
                    isImpactEffectRunning = true;
                }
                if (impactedTarget.IsVisualDetailDiscernibleToUser || DebugControls.Instance.AlwaysHearWeaponImpacts) {
                    HearImpactEffect(contactPoint.point);
                }
            }
        }
        else {
            // if not an attackableTarget, then??   IMPROVE
            var otherOrdnance = impactedGo.GetComponent<AOrdnance>();
            D.AssertNull(otherOrdnance);  // should not be able to impact another piece of ordnance as both are on Ordnance layer
        }

        if (IsOperational && !isImpactEffectRunning) {
            // ordnance has not already been terminated by other paths such as the death of the target and impact effect is not running
            TerminateNow();
        }
    }

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        D.AssertEqual(default(Vector3), _rigidbody.velocity);
        D.Assert(!enabled);
        D.AssertNull(_impactEffectCompletionJob);
        D.AssertNull(_muzzleEffectCompletionJob);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        D.AssertNull(_impactEffectCompletionJob);
        D.AssertNull(_muzzleEffectCompletionJob);
    }

    #endregion

    // 8.12.16 Job pausing moved to JobManager to consolidate pause handling

    protected override float GetDistanceTraveled() {
        return Vector3.Distance(Position, _launchPosition);
    }

    protected override void PrepareForTermination() {
        base.PrepareForTermination();
        if (_muzzleEffect.activeSelf) {
            _muzzleEffect.SetActive(false);
        }
        if (_impactEffect.isPlaying) {
            // ordnance was terminated by other paths such as the death of the target
            _impactEffect.Stop();
        }
        KillImpactEffectCompletionJob();
        KillMuzzleEffectCompletionJob();
        // FIXME what about audio?
    }

    private void KillMuzzleEffectCompletionJob() {
        if (_muzzleEffectCompletionJob != null) {
            _muzzleEffectCompletionJob.Kill();
            _muzzleEffectCompletionJob = null;
        }
    }

    private void KillImpactEffectCompletionJob() {
        if (_impactEffectCompletionJob != null) {
            _impactEffectCompletionJob.Kill();
            _impactEffectCompletionJob = null;
        }
    }

    protected override void ResetEffectsForReuse() {
        // reattach to projectile for reuse
        UnityUtility.AttachChildToParent(_muzzleEffect, gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;

        if (_operatingEffect != null) {
            _operatingEffect.Clear();
            // operatingEffect stays as a child of this projectile and doesn't change position or rotation
        }
        else {
            // if icon has been destroyed, it won't be created again when reused. This will throw an error if not present
#pragma warning disable 0219
            IWorldTrackingSprite operatingIcon = gameObject.GetSingleInterfaceInChildren<IWorldTrackingSprite>();
#pragma warning restore 0219
        }

        __TryRestoreScaleOfImpactEffect();
        _impactEffect.transform.localPosition = Vector3.zero;
        _impactEffect.transform.localRotation = Quaternion.identity;
        _impactEffect.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        // 12.8.16 Job Disposal centralized in JobManager
        KillImpactEffectCompletionJob();
        KillMuzzleEffectCompletionJob();
    }

    #region Debug

    protected override void __ExecuteImpactEffectScaleReduction() {
        ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleReductionFactor, includeChildren: true);   // HACK .01F was used by VisualEffectScale
    }

    protected override void __ExecuteImpactEffectScaleRestoration() {
        ParticleScaler.Scale(_impactEffect, __ImpactEffectScaleRestoreFactor, includeChildren: true);   // HACK
    }

    #endregion

    #region AdjustHeadingForInaccuracy Archive

    // This version has nothing wrong with it. I just think the other is more logical.

    //private void AdjustHeadingForInaccuracy() {
    //    Quaternion initialRotation = transform.rotation;
    //    Vector3 initialEulerHeading = initialRotation.eulerAngles;
    //    float inaccuracyInDegrees = Weapon.MaxLaunchInaccuracy;
    //    float newHeadingX = initialEulerHeading.x + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
    //    float newHeadingY = initialEulerHeading.y + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
    //    Vector3 adjustedEulerHeading = new Vector3(newHeadingX, newHeadingY, initialEulerHeading.z);
    //    transform.rotation = Quaternion.Euler(adjustedEulerHeading);
    //    //D.Log(ShowDebugLog, "{0} has incorporated {1:0.0} degrees of inaccuracy into its trajectory.", DebugName, Quaternion.Angle(initialRotation, transform.rotation));
    //}

    #endregion

}

