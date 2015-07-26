// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SeekerProjectile.cs
// Projectile ordnance containing effects for flight, impact and impact sound.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Projectile ordnance containing effects for flight, impact and impact sound.
/// </summary>
public class SeekerProjectile : AOrdnance, IProjectileOrdnance {

    //private static int __instanceCount = 1;
    private static Vector3 _localSpaceForward = Vector3.forward;

    /// <summary>
    /// The speed of the projectile in units per hour.
    /// </summary>
    [Tooltip("Speed in Units/Hour")]
    public float speed;
    public ParticleSystem inFlightEffect;
    public ParticleSystem impactEffect;
    public Transform muzzleEffect;
    public AudioClip impactClip;


    //private int __instanceID;
    private float _gameSpeedMultiplier;
    private Vector3 _velocityOnPause;
    /// <summary>
    /// The force propelling this projectile, using a gameSpeedMultiplier of 1.
    /// ProjectileMass * ProjectileSpeed (distanceInUnits/hour) * hoursPerSecond * _localTravelDirection
    /// </summary>
    private Vector3 _nominalThrust;
    private float _rangeSqrd;
    private Vector3 _launchPosition;
    private Rigidbody _rigidbody;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        //__instanceID = __instanceCount;
        //__instanceCount++;
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.drag = Constants.ZeroF;
        _rigidbody.useGravity = false;
        Validate();

        //Name = _transform.name + __instanceID;
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        _nominalThrust = CalculateNominalThrust();
        UpdateRate = FrameUpdateFrequency.Seldom;
        Subscribe();
        enabled = false;
    }

    private void Validate() {
        D.Assert(_rigidbody.mass != Constants.ZeroF, "{0} mass not set.".Inject(Name));
        D.Assert(impactClip != null, "{0} has no impact audio clip.".Inject(Name));
        D.Assert(impactEffect != null, "{0} has no impact effect.".Inject(Name));
        D.Assert(inFlightEffect != null, "{0} has no inFlight effect.".Inject(Name));
        D.Assert(!inFlightEffect.playOnAwake);
        D.Assert(impactEffect.playOnAwake);
        D.Assert(!impactEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, impactEffect.name));
        D.Assert(muzzleEffect != null, "{0} has no muzzle effect.".Inject(Name));
        D.Assert(!muzzleEffect.gameObject.activeSelf, "{0}.{1} should not start active.".Inject(GetType().Name, muzzleEffect.name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        D.Assert(speed != Constants.ZeroF);
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
    }

    public override void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        base.Initiate(target, weapon, toShowEffects);
        weapon.OnFiringInitiated(target, this);
        _launchPosition = _transform.position;
        _rangeSqrd = _range * _range;

        if (toShowEffects) {
            ShowMuzzleEffect();
        }
        enabled = true;
        //D.Log("{0} launching from {1} on Heading: {2} (resultingRotation = {3}), Range: {4}.", _transform.name, _launchPosition, heading, _transform.rotation, range);
        //OnWeaponFiringComplete();
        weapon.OnFiringComplete(this);
    }
    //public void Initiate(IElementAttackableTarget intendedTarget, Vector3 heading, float range, CombatStrength strength, bool showEffect) {
    //    __intendedTarget = intendedTarget;
    //    _launchPosition = _transform.position;
    //    _transform.rotation = Quaternion.LookRotation(heading);
    //    //D.Log("{0} launching from {1} on Heading: {2} (resultingRotation = {3}), Range: {4}.", _transform.name, _launchPosition, heading, _transform.rotation, range);
    //    _rangeSqrd = range * range;
    //    _strength = strength;
    //    if (showEffect) {
    //        ShowProjectileEffect(EffectControl.Show);
    //    }
    //    enabled = true;
    //}

    /// <summary>
    /// Calculates the thrust propelling this projectile. Nominal refers to use of a gameSpeedMultiplier of 1.
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateNominalThrust() {
        return _rigidbody.mass * speed * GameTime.HoursPerSecond * _localSpaceForward;
    }

    private void ShowMuzzleEffect() {
        muzzleEffect.gameObject.SetActive(true);    // effect will destroy itself when completed
    }

    private void ShowInflightEffect(EffectControl effectControl) {
        //D.Log("{0}.ShowInflightEffect({1}) called.", Name, effectControl.GetName());
        switch (effectControl) {
            case EffectControl.Show:
                inFlightEffect.Play();
                break;
            case EffectControl.Hide:
                if (inFlightEffect.IsAlive()) {
                    inFlightEffect.Stop();
                    inFlightEffect.Clear();
                }
                break;
            case EffectControl.Pause:
                if (inFlightEffect.isPlaying && !inFlightEffect.isPaused) {
                    inFlightEffect.Pause();
                }
                break;
            case EffectControl.Resume:
                if (inFlightEffect.isPaused) {
                    inFlightEffect.Play();
                }
                break;
            case EffectControl.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(effectControl));
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        CheckRange();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
        ApplyThrust();
    }

    protected override void OnCollisionEnter(Collision collision) {
        base.OnCollisionEnter(collision);
        D.Log("{0}.OnCollisionEnter() called from layer {1}. Collided with {2} on layer {3}.",
            Name, ((Layers)(gameObject.layer)).GetValueName(), collision.collider.name, ((Layers)collision.collider.gameObject.layer).GetValueName());
        D.Log("{0} distance to intended target on collision: {1}.", Name, Vector3.Distance(_transform.position, Target.Position));
        var collidedGo = collision.collider.gameObject;
        var collidedTgt = collidedGo.GetInterface<IElementAttackableTarget>();
        if (collidedTgt != null) {
            // hit an attackableTarget
            D.Log("{0} collided with {1}.", Name, collidedTgt.DisplayName);
            ContactPoint contactPoint = collision.contacts[0];
            var collidedTgtRigidbody = collidedGo.GetComponent<Rigidbody>();
            if (collidedTgtRigidbody != null && !collidedTgtRigidbody.isKinematic) {
                // target has a rigidbody so apply impact force
                var force = _nominalThrust * _gameSpeedMultiplier;
                D.Log("{0} applying impact force of {1} to {2}.", Name, force, collidedTgt.DisplayName);
                collidedTgtRigidbody.AddForceAtPosition(force, contactPoint.point, ForceMode.Impulse);
            }
            if (collidedTgt.IsVisualDetailDiscernibleToUser) {
                // target is being viewed by user so show impact effect
                D.Log("{0} starting impact effect on {1}.", Name, collidedTgt.DisplayName);
                var impactEffectLocation = contactPoint.point + contactPoint.normal * 0.05F;    // HACK
                // IMPROVE = Quaternion.FromToRotation(Vector3.up, contact.normal); // see http://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
                var impactEffectRotation = Quaternion.identity;

                var impactEffectGo = Instantiate<GameObject>(impactEffect.gameObject, impactEffectLocation, impactEffectRotation);
                impactEffectGo.SetActive(true); // impactEffect auto destroyed on completion
                SimpleAudioManager.Instance.Play(impactClip);
            }
            collidedTgt.TakeHit(Strength);
        }
        else {
            // if not an attackableTarget, then it might be another incoming or outgoing projectile. If so, ignore it
            var otherProjectile = collidedGo.GetComponent<SeekerProjectile>();
            if (otherProjectile != null) {
                return;
            }
        }
        Terminate();
    }

    private void ApplyThrust() {
        var gameSpeedAdjustedThrust = _nominalThrust * _gameSpeedMultiplier;
        _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust, ForceMode.Impulse);
        //D.Log("{0} applying thrust of {1}. Velocity is now {2}.", _transform.name, Force, _rigidbody.velocity.magnitude);
    }

    private void CheckRange() {
        var distanceTraveledSqrd = Vector3.SqrMagnitude(_transform.position - _launchPosition);
        //D.Log("{0} distanceTraveledSqrd = {1}.", _transform.name, distanceTraveledSqrd);
        if (distanceTraveledSqrd > _rangeSqrd) {
            Terminate();
        }
    }

    protected override void OnToShowEffectsChanged() {
        if (ToShowEffects) {
            ShowInflightEffect(EffectControl.Show);
        }
        else {
            ShowInflightEffect(EffectControl.Hide);
        }
    }

    private void OnGameSpeedChanged() {
        float previousGameSpeedMultiplier = _gameSpeedMultiplier;
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
        AdjustForGameSpeed(gameSpeedChangeRatio);
    }

    private void OnIsPausedChanged() {
        if (_gameMgr.IsPaused) {
            _velocityOnPause = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
            ShowInflightEffect(EffectControl.Pause);
        }
        else {
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityOnPause;
            _rigidbody.WakeUp();
            ShowInflightEffect(EffectControl.Resume);
        }
    }

    private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
        if (_gameMgr.IsPaused) {
            D.Assert(_velocityOnPause != default(Vector3), "{0} has not yet recorded VelocityOnPause.".Inject(_transform.name));
            _velocityOnPause *= gameSpeedChangeRatio;
        }
        else {
            _rigidbody.velocity *= gameSpeedChangeRatio;
        }
    }

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

