// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;

/// <summary>
/// Abstract base class for Beam, Missile and Projectile Ordnance. 
/// </summary>
public abstract class AOrdnance : AMonoBase, IOrdnance {

    private const string NameFormat = "{0}_{1}";
    private static int __InstanceCount = 1;

    public event EventHandler deathOneShot;

    public string Name { get { return transform.name; } }

    public string FullName { get { return Weapon != null ? NameFormat.Inject(Weapon.RangeMonitor.ParentItem.FullName, Name) : Name; } }

    private IElementAttackable _target;
    public IElementAttackable Target {
        get { return _target; }
        private set {
            SetProperty<IElementAttackable>(ref _target, value, "Target");
        }
    }

    public Vector3 CurrentHeading { get { return transform.forward; } }

    public Player Owner { get { return Weapon.Owner; } }

    private bool _isOperational;
    public bool IsOperational {
        get { return _isOperational; }
        private set { SetProperty<bool>(ref _isOperational, value, "IsOperational"); }
    }

    private WDVStrength _deliveryVehicleStrength;
    public WDVStrength DeliveryVehicleStrength {
        get { return _deliveryVehicleStrength; }
        protected set { SetProperty<WDVStrength>(ref _deliveryVehicleStrength, value, "DeliveryVehicleStrength"); }
    }

    public DamageStrength DamagePotential { get { return Weapon.DamagePotential; } }

    protected virtual bool ToShowMuzzleEffects { get { return IsWeaponDiscernibleToUser; } }

    protected bool IsWeaponDiscernibleToUser { get { return Weapon.IsWeaponDiscernibleToUser; } }

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
    private string _rootName;
    private bool _isNameInitialized;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        _gameTime = GameTime.Instance;
        _subscriptions = new List<IDisposable>();
        enabled = false;
    }

    private void InitializeName() {
        //D.Log("InitializeName before renaming. Name = {0}.", Name);
        string pooledPrefabName = Name;
        string subStringToRemove = "(Clone)";
        int index = pooledPrefabName.IndexOf(subStringToRemove);
        string prefabNameSansClone = index < 0 ? pooledPrefabName : pooledPrefabName.Remove(index, subStringToRemove.Length);
        transform.name = prefabNameSansClone;
        _rootName = prefabNameSansClone;
        //D.Log("InitializeName after renaming. Name = {0}.", Name);
        _isNameInitialized = true;
    }

    protected void PrepareForLaunch(IElementAttackable target, AWeapon weapon) {
        //D.Log("{0} is assigning target {1}.", Name, target.FullName);
        Target = target;
        Weapon = weapon;
        Subscribe();

        DeliveryVehicleStrength = weapon.DeliveryVehicleStrength;

        AssignName();
        weapon.HandleFiringInitiated(target, this);

        _range = weapon.RangeDistance;
        IsOperational = true;
    }

    protected virtual void Subscribe() {
        D.Assert(_subscriptions.IsNullOrEmpty());
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
        _subscriptions.Add(Weapon.SubscribeToPropertyChanged<AWeapon, bool>(weap => weap.IsWeaponDiscernibleToUser, IsWeaponDiscernibleToUserPropChangedHandler));
    }

    protected abstract void AssessShowMuzzleEffects();

    protected void ShowImpactEffects(Vector3 position) { ShowImpactEffects(position, Quaternion.identity); }

    protected abstract void ShowImpactEffects(Vector3 position, Quaternion rotation);

    #region Event and Property Change Handlers

    protected virtual void OnSpawned() {
        if (!_isNameInitialized) {
            InitializeName();
        }
        D.Assert(Target == null);
        D.Assert(Weapon == null);
        D.Assert(__instanceID == Constants.Zero);
        D.Assert(_range == Constants.ZeroF);
        D.Assert(!IsOperational);
        __instanceID = __InstanceCount;
        __InstanceCount++;
    }

    protected virtual void IsWeaponDiscernibleToUserPropChangedHandler() {
        AssessShowMuzzleEffects();
    }

    protected virtual void IsPausedPropChangedHandler() {
        enabled = !_gameMgr.IsPaused;
    }

    private void OnDeath() {
        if (deathOneShot != null) {
            deathOneShot(this, new EventArgs());
            deathOneShot = null;
        }
    }

    protected virtual void OnDespawned() {
        //D.Log("{0}.OnDespawned called.", Name);
        Unsubscribe();
        Target = null;
        Weapon = null;
        __instanceID = Constants.Zero;
        _range = Constants.ZeroF;
        RestoreRootName();
    }

    #endregion

    protected void ReportTargetHit() {
        //D.Log("{0}.ReportTargetHit called.", Name);
        Weapon.HandleTargetHit(Target);
    }

    protected void ReportTargetMissed() {
        //D.Log("{0}.ReportTargetMissed called.", Name);
        Weapon.HandleTargetMissed(Target);
    }

    /// <summary>
    /// Called when this ordnance has been fatally interdicted
    /// by either a Countermeasure (ActiveCM or Shield) or another
    /// object that was not its target.
    /// </summary>
    protected void ReportInterdiction() {
        //D.Log("{0}.ReportInterdiction called.", Name);
        Weapon.HandleOrdnanceInterdicted(Target);
    }

    /// <summary>
    /// Adds __instanceID to the root name.
    /// </summary>
    private void AssignName() {
        transform.name = NameFormat.Inject(_rootName, __instanceID);
    }

    private void RestoreRootName() {
        transform.name = _rootName;
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
        ResetEffectsForReuse();

        OnDeath();
        Despawn();
    }

    protected virtual void Despawn() {
        //D.Log("{0} is about to despawn.", Name);
        MyPoolManager.Instance.DespawnOrdnance(transform);
    }

    /// <summary>
    /// Called while terminating, this is a derived class'
    /// opportunity to do any cleanup (stop audio, etc.) prior to the gameObject being despawned.
    /// </summary>
    protected virtual void PrepareForTermination() { }

    /// <summary>
    /// Called after PrepareForTermination, all derived classes must reset each of their effects 
    /// so they are ready for reuse when Spawned again.
    /// <remarks>This method is only for preparing for reuse. Stopping effects should be handled by PrepareForTermination.</remarks>
    /// </summary>
    protected abstract void ResetEffectsForReuse();

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    #endregion

}

