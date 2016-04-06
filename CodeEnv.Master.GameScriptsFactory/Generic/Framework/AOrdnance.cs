// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOrdnance.cs
// Abstract base class for Beam, Missile and Projectile Ordnance.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Beam, Missile and Projectile Ordnance. 
/// </summary>
public abstract class AOrdnance : AMonoBase, IOrdnance {

    private static int __instanceCount = 1;
    private static string _fullNameFormat = "{0}_{1}";

    public event EventHandler deathOneShot;

    public string Name { get; private set; }

    public string FullName { get { return _fullNameFormat.Inject(Weapon.RangeMonitor.ParentItem.FullName, Name); } }

    private IElementAttackableTarget _target;
    public IElementAttackableTarget Target {
        get { return _target; }
        private set { SetProperty<IElementAttackableTarget>(ref _target, value, "Target"); }
    }

    public Vector3 CurrentHeading { get { return transform.forward; } }

    public Player Owner { get { return Weapon.Owner; } }

    private bool _isOperational;
    public bool IsOperational {
        get { return _isOperational; }
        private set { SetProperty<bool>(ref _isOperational, value, "IsOperational"); }
    }

    private bool _toShowEffects;
    public bool ToShowEffects {
        get { return _toShowEffects; }
        set { SetProperty<bool>(ref _toShowEffects, value, "ToShowEffects", ToShowEffectsPropChangedHandler); }
    }

    private WDVStrength _deliveryVehicleStrength;
    public WDVStrength DeliveryVehicleStrength {
        get { return _deliveryVehicleStrength; }
        protected set { SetProperty<WDVStrength>(ref _deliveryVehicleStrength, value, "DeliveryVehicleStrength"); }
    }

    public DamageStrength DamagePotential { get { return Weapon.DamagePotential; } }

    private AWeapon _weapon;
    protected AWeapon Weapon {
        get { return _weapon; }
        private set { SetProperty<AWeapon>(ref _weapon, value, "Weapon"); }
    }

    protected float _range;
    protected GameManager _gameMgr;
    protected GameTime _gameTime;
    protected IList<IDisposable> _subscriptions;
    private int __instanceID;

    protected override void Awake() {
        base.Awake();
        __instanceID = __instanceCount;
        __instanceCount++;
        _gameMgr = GameManager.Instance;
        _gameTime = GameTime.Instance;
        Subscribe();
        enabled = false;
    }

    protected virtual void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
    }

    protected void PrepareForLaunch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        Target = target;
        Weapon = weapon;

        DeliveryVehicleStrength = weapon.DeliveryVehicleStrength;

        SyncName();
        weapon.HandleFiringInitiated(target, this);

        _range = weapon.RangeDistance;
        ToShowEffects = toShowEffects;
        IsOperational = true;
    }

    protected abstract void AssessShowMuzzleEffects();

    protected abstract void AssessShowOperatingEffects();

    protected void ShowImpactEffects(Vector3 position) { ShowImpactEffects(position, Quaternion.identity); }

    protected abstract void ShowImpactEffects(Vector3 position, Quaternion rotation);

    protected virtual void ToShowEffectsPropChangedHandler() {
        //D.Log("{0}.ToShowEffects is now {1}.", Name, ToShowEffects);
        AssessShowMuzzleEffects();
        AssessShowOperatingEffects();
    }

    #region Event and Property Change Handlers

    protected virtual void IsPausedPropChangedHandler() {
        enabled = !_gameMgr.IsPaused;
    }

    private void OnDeath() {
        if (deathOneShot != null) {
            deathOneShot(this, new EventArgs());
            deathOneShot = null;
        }
    }

    #endregion

    protected void ReportTargetHit() {
        Weapon.HandleTargetHit(Target);
    }

    protected void ReportTargetMissed() {
        Weapon.HandleTargetMissed(Target);
    }

    /// <summary>
    /// Called when this ordnance has been fatally interdicted
    /// by either a Countermeasure (ActiveCM or Shield) or another
    /// object that was not its target.
    /// </summary>
    protected void ReportInterdiction() {
        Weapon.HandleOrdnanceInterdicted(Target);
    }

    /// <summary>
    /// Synchronizes Name and transform's name and adds instanceID.
    /// Must be called after Awake() as UnityUtility.AddChild can't get rid of "Clone" until after Awake runs.
    /// </summary>
    private void SyncName() {
        Name = transform.name + __instanceID;
        transform.name = Name;
    }

    protected void TerminateNow() {
        if (!IsOperational) {
            D.Warn("{0}.TerminateNow called when already terminating.", Name);
            return;
        }
        //D.Log("{0} is terminating.", Name); 
        enabled = false;
        IsOperational = false;
        PrepareForTermination();

        OnDeath();
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when this ordnance is about to be terminated, this is a derived class'
    /// opportunity to do any cleanup (stop audio, etc.) prior to the gameObject being destroyed.
    /// </summary>
    protected virtual void PrepareForTermination() { }

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    #endregion

}

