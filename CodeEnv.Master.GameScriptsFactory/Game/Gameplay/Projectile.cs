// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Projectile.cs
// Unguided AProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Unguided AProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact. 
/// </summary>
public class Projectile : AProjectileOrdnance {

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

    private bool IsWaitForImpactEffectCompletionJobRunning { get { return _waitForImpactEffectCompletionJob != null && _waitForImpactEffectCompletionJob.IsRunning; } }

    private bool IsWaitForMuzzleEffectCompletionJobRunning { get { return _waitForMuzzleEffectCompletionJob != null && _waitForMuzzleEffectCompletionJob.IsRunning; } }

    private Job _waitForImpactEffectCompletionJob;
    private Job _waitForMuzzleEffectCompletionJob;

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        AdjustHeadingForInaccuracy();
        InitializeVelocity();
        enabled = true;
    }

    protected override void ValidateEffects() {
        D.Assert(_muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!_muzzleEffect.activeSelf, "{0}.{1} should not start active.", GetType().Name, _muzzleEffect.name);
        if (_operatingEffect != null) {
            // ParticleSystem Operating Effect can be null. If so, it will be replaced by an Icon
            D.Assert(!_operatingEffect.playOnAwake);
            D.Assert(_operatingEffect.loop);
        }
        D.Assert(_impactEffect != null, "{0} has no impact effect.", Name);
        D.Assert(!_impactEffect.playOnAwake);   // Awake only called once during GameObject life -> can't use with pooling
        D.Assert(_impactEffect.gameObject.activeSelf, "{0}.{1} should start active.", GetType().Name, _impactEffect.name);
    }

    protected override AProjectileDisplayManager MakeDisplayMgr() {
        return new ProjectileDisplayManager(this, Layers.Cull_15, _operatingEffect);
    }

    private void AdjustHeadingForInaccuracy() {
        Quaternion initialRotation = transform.rotation;
        Vector3 initialEulerHeading = initialRotation.eulerAngles;
        float inaccuracyInDegrees = Weapon.MaxLaunchInaccuracy;
        float newHeadingX = initialEulerHeading.x + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
        float newHeadingY = initialEulerHeading.y + UnityEngine.Random.Range(-inaccuracyInDegrees, inaccuracyInDegrees);
        Vector3 adjustedEulerHeading = new Vector3(newHeadingX, newHeadingY, initialEulerHeading.z);
        transform.rotation = Quaternion.Euler(adjustedEulerHeading);
        //D.Log("{0} has incorporated {1:0.0} degrees of inaccuracy into its trajectory.", Name, Quaternion.Angle(initialRotation, transform.rotation));
    }

    /// <summary>
    /// One-time initialization of the velocity of this 'projectile'.
    /// </summary>
    private void InitializeVelocity() {
        _rigidbody.velocity = CurrentHeading * MaxSpeed * _gameTime.GameSpeedAdjustedHoursPerSecond;
    }

    protected override void ShowMuzzleEffect() {
        D.Assert(!IsWaitForMuzzleEffectCompletionJobRunning);
        // relocate this Effect so it doesn't move with the projectile while showing
        UnityUtility.AttachChildToParent(_muzzleEffect, DynamicObjectsFolder.Instance.gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;
        _muzzleEffect.transform.position = Position;
        _muzzleEffect.transform.rotation = transform.rotation;
        _muzzleEffect.SetActive(true);
        _waitForMuzzleEffectCompletionJob = WaitJobUtility.WaitForSeconds(0.2F, waitFinished: (jobWasKilled) => {
            _muzzleEffect.SetActive(false);
        });
        //TODO Add audio
    }

    // OPTIMIZE particle system should be at correct scale to begin with so no runtime scaling reqd
    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        base.ShowImpactEffects(position, rotation);
        D.Assert(!_impactEffect.isPlaying); // should not be called more than once
        D.Assert(!IsWaitForImpactEffectCompletionJobRunning);   // should not be called more than once
        D.Assert(IsOperational);
        ParticleScaler.Scale(_impactEffect, __ImpactEffectScalerValue, includeChildren: true);   // HACK .01F was used by VisualEffectScale
        _impactEffect.transform.position = position;
        _impactEffect.transform.rotation = rotation;
        _impactEffect.Play();
        _waitForImpactEffectCompletionJob = WaitJobUtility.WaitForParticleSystemCompletion(_impactEffect, includeChildren: true, waitFinished: (jobWasKilled) => {
            if (IsOperational) {
                // ordnance has not already been terminated by other paths such as the death of the target
                TerminateNow();
            }
        });

        GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
        SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion    // FIXME ??
    }

    protected override void HandleImpactEffectsBegun() {
        base.HandleImpactEffectsBegun();
        // nothing unique to shutdown 
    }

    #region Event and Property Change Handlers

    protected override void OnSpawned() {
        base.OnSpawned();
        D.Assert(_rigidbody.velocity == Vector3.zero);
        D.Assert(!enabled);
        D.Assert(_waitForImpactEffectCompletionJob == null);
        D.Assert(_waitForMuzzleEffectCompletionJob == null);
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        PauseJobs(_gameMgr.IsPaused);
    }

    protected override void OnDespawned() {
        base.OnDespawned();
        _waitForImpactEffectCompletionJob = null;
        _waitForMuzzleEffectCompletionJob = null;
    }

    #endregion

    private void PauseJobs(bool toPause) {
        if (IsWaitForMuzzleEffectCompletionJobRunning) {
            _waitForMuzzleEffectCompletionJob.IsPaused = toPause;
        }
        if (IsWaitForImpactEffectCompletionJobRunning) {
            _waitForImpactEffectCompletionJob.IsPaused = toPause;
        }
    }

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
        if (IsWaitForImpactEffectCompletionJobRunning) {
            _waitForImpactEffectCompletionJob.Kill();
        }
        if (IsWaitForMuzzleEffectCompletionJobRunning) {
            _waitForMuzzleEffectCompletionJob.Kill();
        }
        // FIXME what about audio?
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
            ITrackingSprite operatingIcon = gameObject.GetSingleInterfaceInChildren<ITrackingSprite>();
#pragma warning restore 0219
        }

        ParticleScaler.Scale(_impactEffect, 1F / __ImpactEffectScalerValue, includeChildren: true);   // HACK .01F was used by VisualEffectScale
        _impactEffect.transform.localPosition = Vector3.zero;
        _impactEffect.transform.localRotation = Quaternion.identity;
        _impactEffect.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_waitForImpactEffectCompletionJob != null) {
            _waitForImpactEffectCompletionJob.Dispose();
        }
        if (_waitForMuzzleEffectCompletionJob != null) {
            _waitForMuzzleEffectCompletionJob.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

