﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Missile.cs
// Guided projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
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
/// Guided projectile ordnance containing effects for muzzle flash, inFlightOperation and impact. 
/// </summary>
public class Missile : AProjectile, ITerminatableOrdnance {

    public GameObject muzzleEffect;
    /// <summary>
    /// The effect this Projectile will show while operating including when the game is paused.
    /// </summary>
    public ParticleSystem operatingEffect;
    public ParticleSystem impactEffect;

    private float _cumDistanceTraveled;
    private Vector3 _positionLastRangeCheck;
    private float _weaponAccuracy;

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Infrequent;
    }

    public override void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        base.Initiate(target, weapon, toShowEffects);
        _weaponAccuracy = weapon.Accuracy;
        _positionLastRangeCheck = _transform.position;
    }

    protected override void ValidateEffects() {
        base.ValidateEffects();
        D.Assert(impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(impactEffect.playOnAwake);
        D.Assert(!impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, impactEffect.name));
        D.Assert(operatingEffect != null, "{0} has no inFlight effect.".Inject(Name));
        D.Assert(!operatingEffect.playOnAwake);
        D.Assert(muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!muzzleEffect.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, muzzleEffect.name));
    }

    protected override void AssessShowMuzzleEffects() {
        if (muzzleEffect != null) { // muzzleEffect is detroyed once used
            var toShow = ToShowEffects && !_hasWeaponFired;
            muzzleEffect.SetActive(toShow);    // effect will destroy itself when completed
        }
    }

    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        if (toShow) {
            operatingEffect.Play();
        }
        else {
            operatingEffect.Stop();
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        if (impactEffect != null) { // impactEffect is detroyed once used but method can be called after that
            // relocate this impactEffect as this projectile could be destroyed before the effect is done playing
            UnityUtility.AttachChildToParent(impactEffect.gameObject, DynamicObjectsFolder.Instance.gameObject);
            impactEffect.gameObject.layer = (int)Layers.TransparentFX;
            impactEffect.transform.position = position;
            impactEffect.transform.rotation = rotation;
            impactEffect.gameObject.SetActive(true);    // auto destroyed on completion

            GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
            SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        CheckProgress();
    }

    private void CheckProgress() {
        if (!Target.IsOperational) {
            // target is dead and about to be destroyed. GetTargetFiringSolution() will throw errors when destroyed
            D.Log("{0} is self terminating as its Target {1} is dead.", Name, Target.FullName);
            TerminateNow();
            return;
        }

        if (_cumDistanceTraveled < TempGameValues.__ReqdMissileTravelDistanceBeforePushover) {
            // avoid steering until pushover
            return;
        }
        Steer();
    }

    /// <summary>
    /// Keep missile pointed at target.
    /// IMPROVE Should be checked infrequently enough to allow a miss.
    /// </summary>
    private void Steer() {
        Vector3 tgtBearing = (Target.Position - Position).normalized;
        _transform.rotation = Quaternion.LookRotation(tgtBearing);  // TODO needs inaccuracy    // Missile needs maxTurnRate, add deltaTime
    }

    protected override float GetDistanceTraveled() {
        _cumDistanceTraveled += Vector3.Distance(_transform.position, _positionLastRangeCheck);
        _positionLastRangeCheck = _transform.position;
        return _cumDistanceTraveled;
    }

    public void Terminate() {
        TerminateNow();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

