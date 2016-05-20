// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Bullet_UnPooled.cs
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
[Obsolete]
public class Bullet_UnPooled : AProjectileOrdnance_UnPooled {

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
    public override float OpenSpaceDrag { get { return Weapon.OrdnanceDrag; } }

    public override float Mass { get { return Weapon.OrdnanceMass; } }

    protected new ProjectileLauncher Weapon { get { return base.Weapon as ProjectileLauncher; } }

    public override void Launch(IElementAttackable target, AWeapon weapon, Topography topography) {
        base.Launch(target, weapon, topography);
        InitializeVelocity();
        enabled = true;
    }

    protected override void ValidateEffects() {
        D.Assert(_impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(_impactEffect.playOnAwake);
        D.Assert(!_impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _impactEffect.name));
        D.Assert(_muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!_muzzleEffect.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, _muzzleEffect.name));
        if (_operatingEffect != null) {
            // ParticleSystem Operating Effect can be null. If so, it will be replaced by an Icon
            D.Assert(!_operatingEffect.playOnAwake);
        }
    }

    protected override AProjectileDisplayManager MakeDisplayMgr() {
        return new BulletDisplayManager(this, Layers.Cull_15, _operatingEffect);
    }

    /// <summary>
    /// One-time initialization of the velocity of this 'bullet'.
    /// </summary>
    private void InitializeVelocity() {
        _rigidbody.velocity = CurrentHeading * MaxSpeed * _gameTime.GameSpeedAdjustedHoursPerSecond;
    }

    protected override void ShowMuzzleEffect() {
        // relocate this Effect so it doesn't move with the projectile while showing
        UnityUtility.AttachChildToParent(_muzzleEffect, DynamicObjectsFolder.Instance.gameObject);
        _muzzleEffect.layer = (int)Layers.TransparentFX;
        _muzzleEffect.transform.position = Position;
        _muzzleEffect.transform.rotation = transform.rotation;
        _muzzleEffect.SetActive(true);    // auto destroyed on completion
        //TODO Add audio
    }

    protected override void ShowImpactEffects(Vector3 position, Quaternion rotation) {
        if (_impactEffect != null) { // impactEffect is destroyed once used but method can be called after that
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

    protected override float GetDistanceTraveled() {
        return Vector3.Distance(Position, _launchPosition);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

