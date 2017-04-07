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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
/// <remarks>2.15.17 Added IEquatable to allow pool-generated instances to be used in Dictionary and HashSet.
/// Without it, a reused instance appears to be equal to another reused instance if from the same instance. Probably doesn't matter
/// as only 1 reused instance from an instance can exist at the same time, but...</remarks>
/// </summary>
public abstract class AOrdnance : AMonoBase, IOrdnance, IEquatable<AOrdnance> {

    private const string DebugNameFormat = "{0}_{1}";
    private const string NameSubstringToRemove = "(Clone)";
    private static int _UniqueIDCount = 1;

    public event EventHandler terminationOneShot;

    public string Name { get; private set; }    // 12.10.16 return transform.name generated 'transform destroyed' error on editor exit

    public string DebugName {
        get {
            if (Weapon == null) {
                return Name;
            }
            // 12.10.16 Can't cache as name changes with reuse
            return DebugNameFormat.Inject(Weapon.RangeMonitor.ParentItem.DebugName, Name);
        }
    }

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

    public bool ShowDebugLog { get { return DebugControls.Instance.ShowOrdnanceDebugLogs; } }

    protected virtual bool ToShowMuzzleEffects { get { return IsWeaponDiscernibleToUser; } }

    protected bool IsWeaponDiscernibleToUser { get { return Weapon.IsWeaponDiscernibleToUser; } }

    protected abstract Layers Layer { get; }

    private AWeapon _weapon;
    protected AWeapon Weapon {
        get { return _weapon; }
        private set { SetProperty<AWeapon>(ref _weapon, value, "Weapon"); }
    }

    protected float _range;
    protected IGameManager _gameMgr;
    protected GameTime _gameTime;
    protected IJobManager _jobMgr;
    protected IList<IDisposable> _subscriptions;

    private int _uniqueID;
    private string _rootName;
    private bool _isNameInitialized;
    private bool _toDespawn;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameReferences.GameManager;
        _gameTime = GameTime.Instance;
        _jobMgr = GameReferences.JobManager;
        _subscriptions = new List<IDisposable>(3);
        ValidateLayer();
        enabled = false;
    }

    private void ValidateLayer() {
        D.AssertEqual(Layer, (Layers)gameObject.layer, DebugName);
    }

    private void InitializeName() {
        // 12.10.16 Do not access DebugName yet
        string pooledPrefabName = transform.name;
        int index = pooledPrefabName.IndexOf(NameSubstringToRemove);
        string prefabNameSansClone = index < 0 ? pooledPrefabName : pooledPrefabName.Remove(index, NameSubstringToRemove.Length);
        _rootName = prefabNameSansClone;
        transform.name = _rootName;
        Name = _rootName;
        //D.Log(ShowDebugLog, "InitializeName after renaming. Name = {0}.", DebugName);
        _isNameInitialized = true;
    }

    protected void PrepareForLaunch(IElementAttackable target, AWeapon weapon) {
        //D.Log(ShowDebugLog, "{0} is assigning target {1}.", DebugName, target.DebugName);
        Target = target;
        Weapon = weapon;
        Subscribe();

        DeliveryVehicleStrength = weapon.DeliveryVehicleStrength;

        AssignName();
        weapon.HandleFiringInitiated(target, this);

        _range = weapon.RangeDistance;
        IsOperational = true;

        enabled = true;
    }

    protected virtual void Subscribe() {
        D.AssertNotNull(_subscriptions);
        D.Assert(_subscriptions.Count == Constants.Zero);
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
        _subscriptions.Add(Weapon.SubscribeToPropertyChanged<AWeapon, bool>(weap => weap.IsWeaponDiscernibleToUser, IsWeaponDiscernibleToUserPropChangedHandler));
    }

    protected abstract void AssessShowMuzzleEffects();

    protected void ShowImpactEffects(Vector3 position) { ShowImpactEffects(position, Quaternion.identity); }

    protected abstract void ShowImpactEffects(Vector3 position, Quaternion rotation);

    #region Event and Property Change Handlers

    void Update() {
        if (_toDespawn) {
            enabled = false;
            Despawn();
        }
        else {
            ProcessUpdate();
        }
    }

    protected virtual void ProcessUpdate() { }

    protected virtual void OnSpawned() {
        if (!_isNameInitialized) {
            InitializeName();
        }
        D.AssertNull(Target);
        D.AssertNull(Weapon);
        D.AssertDefault(_uniqueID);
        D.AssertDefault(_range);
        D.Assert(!IsOperational);
        D.Assert(!_toDespawn);

        _uniqueID = _UniqueIDCount;
        _UniqueIDCount++;
    }

    protected virtual void IsWeaponDiscernibleToUserPropChangedHandler() {
        AssessShowMuzzleEffects();
    }

    protected virtual void IsPausedPropChangedHandler() {
        enabled = !_gameMgr.IsPaused;
    }

    private void OnTermination() {
        if (terminationOneShot != null) {
            terminationOneShot(this, EventArgs.Empty);
            terminationOneShot = null;
        }
    }

    protected virtual void OnDespawned() {
        //D.Log(ShowDebugLog, "{0}.OnDespawned called.", DebugName);
        Target = null;
        Weapon = null;
        D.AssertNotEqual(Constants.Zero, _uniqueID);
        D.Assert(!enabled); // should be disabled just before Despawn is called
        _uniqueID = Constants.Zero;
        _range = Constants.ZeroF;

        D.Assert(_toDespawn);
        _toDespawn = false;
        // RootName is restored after returning to pool so that it doesn't show in Unity with its most recent _uniqueID name
        RestoreRootName();    // Remove when debugging a problem where despawning is occurring before you expect it and you need its uniqueID
    }

    #endregion

    protected void ReportTargetHit() {
        //D.Log(ShowDebugLog, "{0}.ReportTargetHit called.", DebugName);
        Weapon.HandleTargetHit(Target);
    }

    protected void ReportTargetMissed() {
        //D.Log(ShowDebugLog, "{0}.ReportTargetMissed called.", DebugName);
        Weapon.HandleTargetMissed(Target);
    }

    /// <summary>
    /// Called when this ordnance has been fatally interdicted
    /// by either a Countermeasure (ActiveCM or Shield) or another
    /// object that was not its target.
    /// </summary>
    protected void ReportInterdiction() {
        //D.Log(ShowDebugLog, "{0}.ReportInterdiction called.", DebugName);
        Weapon.HandleOrdnanceInterdicted(Target);
    }

    /// <summary>
    /// Adds __instanceID to the root name.
    /// </summary>
    private void AssignName() {
        transform.name = DebugNameFormat.Inject(_rootName, _uniqueID);
        Name = transform.name;
    }

    private void RestoreRootName() {
        transform.name = _rootName;
        Name = _rootName;
    }

    protected void TerminateNow() {
        if (!IsOperational) {
            D.Warn("{0}.TerminateNow called when already terminating.", DebugName);
            return;
        }
        //D.Log(ShowDebugLog, "{0} is terminating.", DebugName); 
        IsOperational = false;
        Unsubscribe();  // 4.6.17 Moved here from OnDespawned as events (pause...) could still arrive in 1 frame gap

        PrepareForTermination();
        ResetEffectsForReuse();

        OnTermination();
        _toDespawn = true;
    }

    protected virtual void Despawn() {
        //D.Log(ShowDebugLog, "{0} is about to despawn.", DebugName);
        GameReferences.GamePoolManager.DespawnOrdnance(transform);
    }

    /// <summary>
    /// Called while terminating, this is a derived class'
    /// opportunity to do any cleanup (stop audio, etc.) prior to the gameObject being despawned.
    /// <remarks>Being 'despawned' by the PoolManager initiates the following sequence:
    /// 1. a potential change in parent,
    /// 2. broadcasting a OnDespawned message via SendMessage, and
    /// 3. deactivation of the gameObject.</remarks>
    /// </summary>
    protected virtual void PrepareForTermination() { }

    /// <summary>
    /// Called after PrepareForTermination, all derived classes must reset each of their effects 
    /// so they are ready for reuse when Spawned again.
    /// <remarks>This method is only for preparing for reuse. Stopping effects should be handled by PrepareForTermination.</remarks>
    /// </summary>
    protected abstract void ResetEffectsForReuse();

    #region Object.Equals and GetHashCode Override

    public override bool Equals(object obj) {
        if (!(obj is AOrdnance)) { return false; }
        return Equals((AOrdnance)obj);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// See "Page 254, C# 4.0 in a Nutshell."
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode() {
        unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
            int hash = base.GetHashCode();
            hash = hash * 31 + _uniqueID.GetHashCode(); // 31 = another prime number
            return hash;
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    #endregion

    #region IEquatable<AOrdnance> Members

    public bool Equals(AOrdnance other) {
        // if the same instance and _uniqueID are equal, then its the same
        return base.Equals(other) && _uniqueID == other._uniqueID;  // need instance comparison as _uniqueID is 0 in PoolMgr
    }

    #endregion

}

