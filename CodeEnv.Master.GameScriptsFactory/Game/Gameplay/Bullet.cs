﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Bullet.cs
// Unguided AProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.
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
/// Unguided AProjectileOrdnance containing effects for muzzle flash, inFlight operation and impact.
/// </summary>
public class Bullet : AProjectileOrdnance {

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
    /// The maximum speed of this bullet in units per hour in Topography.OpenSpace.
    /// The actual speed of this bullet will be at this MaxSpeed when first fired. As it travels
    /// its speed will decline as the bullet's drag affects it. The bullet's drag will be greater
    /// in higher density Topography causing the bullet's actual speed to decline faster.
    /// </summary>
    public override float MaxSpeed {
        get { return _maxSpeed > Constants.ZeroF ? _maxSpeed : Weapon.OrdnanceMaxSpeed; }
    }

    /// <summary>
    /// The drag of this projectile in Topography.OpenSpace.
    /// </summary>
    public override float Drag { get { return Weapon.OrdnanceDrag; } }

    public override float Mass { get { return Weapon.OrdnanceMass; } }

    protected new ProjectileLauncher Weapon { get { return base.Weapon as ProjectileLauncher; } }

    public override void Launch(IElementAttackableTarget target, AWeapon weapon, Topography topography, bool toShowEffects) {
        base.Launch(target, weapon, topography, toShowEffects);
        InitializeVelocity();
        enabled = true; // enables Update()
    }

    protected override void ValidateEffects() {
        base.ValidateEffects();
        D.Assert(_impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(_impactEffect.playOnAwake);
        D.Assert(!_impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _impactEffect.name));
        D.Assert(_operatingEffect != null, "{0} has no inFlight effect.".Inject(Name));
        D.Assert(!_operatingEffect.playOnAwake);
        D.Assert(_muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!_muzzleEffect.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _muzzleEffect.name));
    }

    /// <summary>
    /// One-time initialization of the velocity of this 'bullet'.
    /// </summary>
    private void InitializeVelocity() {
        _rigidbody.velocity = Heading * MaxSpeed;
    }

    protected override void AssessShowMuzzleEffects() {
        var toShow = ToShowEffects && !_hasWeaponFired;
        ShowMuzzleEffects(toShow);
    }

    private void ShowMuzzleEffects(bool toShow) {
        if (_muzzleEffect != null) { // muzzleEffect is detroyed once used
            _muzzleEffect.SetActive(toShow);    // effect will destroy itself when completed
        }
        // TODO add Audio
    }

    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        ShowOperatingEffects(toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
        if (toShow) {
            _operatingEffect.Play();
        }
        else {
            _operatingEffect.Stop();
        }
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        if (_impactEffect != null) { // impactEffect is detroyed once used but method can be called after that
            // relocate this impactEffect as this projectile could be destroyed before the effect is done playing
            UnityUtility.AttachChildToParent(_impactEffect.gameObject, DynamicObjectsFolder.Instance.gameObject);
            _impactEffect.gameObject.layer = (int)Layers.TransparentFX;
            _impactEffect.transform.position = position;
            _impactEffect.transform.rotation = rotation;
            _impactEffect.gameObject.SetActive(true);    // auto destroyed on completion

            GameObject impactSFXGo = GeneralFactory.Instance.MakeAutoDestruct3DAudioSFXInstance("ImpactSFX", position);
            SFXManager.Instance.PlaySFX(impactSFXGo, SfxGroupID.ProjectileImpacts);  // auto destroyed on completion
        }
    }

    protected override Vector3 GetForceOfImpact() { return _rigidbody.velocity * _rigidbody.mass; }

    protected override float GetDistanceTraveled() {
        return Vector3.Distance(Position, _launchPosition);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

