// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Projectile.cs
// Unguided Projectile ordnance containing effects for muzzle flash, inFlight operation and impact.
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
/// Unguided Projectile ordnance containing effects for muzzle flash, inFlight operation and impact.
/// </summary>
public class Projectile : AProjectile {

    public GameObject muzzleEffect;
    /// <summary>
    /// The effect this Projectile will show while operating including when the game is paused.
    /// </summary>
    public ParticleSystem operatingEffect;
    public ParticleSystem impactEffect;

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
        var toShow = ToShowEffects && !_hasWeaponFired;
        ShowMuzzleEffects(toShow);
    }

    private void ShowMuzzleEffects(bool toShow) {
        if (muzzleEffect != null) { // muzzleEffect is detroyed once used
            muzzleEffect.SetActive(toShow);    // effect will destroy itself when completed
        }
        // TODO add Audio
    }

    protected override void AssessShowOperatingEffects() {
        var toShow = ToShowEffects;
        ShowOperatingEffects(toShow);
    }

    private void ShowOperatingEffects(bool toShow) {
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

    protected override float GetDistanceTraveled() {
        return Vector3.Distance(_transform.position, _launchPosition);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

