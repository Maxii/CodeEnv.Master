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

    public event Action<IOrdnance> onDeathOneShot;

    public string Name { get; private set; }

    public string FullName { get { return _fullNameFormat.Inject(Weapon.RangeMonitor.ParentItem.FullName, Name); } }

    public IElementAttackableTarget Target { get; private set; }

    public Vector3 Heading { get { return transform.forward; } }

    public Player Owner { get { return Weapon.Owner; } }

    public bool IsOperational { get; private set; }

    private bool _toShowEffects;
    public bool ToShowEffects {
        get { return _toShowEffects; }
        set { SetProperty<bool>(ref _toShowEffects, value, "ToShowEffects", OnToShowEffectsChanged); }
    }

    public WDVStrength DeliveryVehicleStrength { get; protected set; }

    public DamageStrength DamagePotential { get; private set; }

    protected AWeapon Weapon { get; private set; }

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
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
    }

    protected void PrepareForLaunch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        Target = target;
        Weapon = weapon;

        DeliveryVehicleStrength = weapon.DeliveryVehicleStrength;
        DamagePotential = weapon.DamagePotential;

        SyncName();
        weapon.OnFiringInitiated(target, this);

        _range = weapon.RangeDistance;
        ToShowEffects = toShowEffects;
        IsOperational = true;
    }

    protected abstract void AssessShowMuzzleEffects();

    protected abstract void AssessShowOperatingEffects();

    protected void ShowImpactEffects(Vector3 position) { ShowImpactEffects(position, Quaternion.identity); }

    protected abstract void ShowImpactEffects(Vector3 position, Quaternion rotation);

    protected virtual void OnToShowEffectsChanged() {
        //D.Log("{0}.ToShowEffects is now {1}.", Name, ToShowEffects);
        AssessShowMuzzleEffects();
        AssessShowOperatingEffects();
    }

    protected virtual void OnIsPausedChanged() {
        enabled = !_gameMgr.IsPaused;
    }

    protected void ReportTargetHit() {
        Weapon.OnTargetHit(Target);
    }

    protected void ReportTargetMissed() {
        Weapon.OnTargetMissed(Target);
    }

    /// <summary>
    /// Called when this ordnance has been fatally interdicted
    /// by either a Countermeasure (ActiveCM or Shield) or another
    /// object that was not its target.
    /// </summary>
    protected void ReportInterdiction() {
        Weapon.OnOrdnanceInterdicted(Target);
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
        if (onDeathOneShot != null) {
            onDeathOneShot(this);
            onDeathOneShot = null;
        }
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

