// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementModel.cs
// Abstract base class for an Element, an object that is under the command of a CommandItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for an Element, an object that is under the command of a CommandItem.
/// </summary>
public abstract class AUnitElementModel : ACombatItemModel, IElementModel, IElementAttackableTarget {

    public virtual bool IsHQElement { get; set; }

    public new AUnitElementItemData Data {
        get { return base.Data as AUnitElementItemData; }
        set { base.Data = value; }
    }

    private AUnitCommandModel _command;
    public AUnitCommandModel Command {
        get { return _command; }
        set { SetProperty<AUnitCommandModel>(ref _command, value, "Command", OnCommandChanged); }
    }

    protected Rigidbody _rigidbody;
    /// <summary>
    /// Weapon Range Monitor lookup table keyed by the Monitor's Guid ID.
    /// </summary>
    protected IDictionary<Guid, IWeaponRangeMonitor> _weaponRangeMonitorLookup;
    protected float _gameSpeedMultiplier;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void InitializeRadiiComponents() {
        // Note: Radius is set in derived classes due to the difference in meshes
        collider.isTrigger = false;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    }

    protected override void Initialize() {
        _rigidbody.mass = Data.Mass;
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        //TODO: Weapon values don't change but weapons do so I need to know when that happens
    }

    private void OnGameSpeedChanged() {
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (enabled) {  // acts just like an isInitialized test as enabled results in Start() which calls Initialize 
            _weaponRangeMonitorLookup.Values.ForAll(rt => rt.Owner = Data.Owner);
        }
    }

    private void OnCommandChanged() {
        // Changing the parentName of this element is handled by the new Command's Data
        if (onCommandChanged != null) {
            onCommandChanged(Command);
        }
    }

    protected override void OnNamingChanged() {
        base.OnNamingChanged();
        _weaponRangeMonitorLookup.Values.ForAll(rt => rt.ParentFullName = Data.FullName);
    }

    #region Weapons

    /// <summary>
    /// Adds the weapon to this element, paired with the provided range monitor. Clients wishing to add
    /// a weapon to this element should use UnitFactory.AddWeapon(weapon, element).
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="rangeMonitor">The range monitor to pair with this weapon.</param>
    public void AddWeapon(AWeapon weapon, IWeaponRangeMonitor rangeMonitor) {
        if (_weaponRangeMonitorLookup == null) {
            _weaponRangeMonitorLookup = new Dictionary<Guid, IWeaponRangeMonitor>();
        }
        if (!_weaponRangeMonitorLookup.ContainsKey(rangeMonitor.ID)) {
            // only need to record and setup range trackers once. The same rangeTracker can have more than 1 weapon
            _weaponRangeMonitorLookup.Add(rangeMonitor.ID, rangeMonitor);
            rangeMonitor.ParentFullName = FullName;
            rangeMonitor.RangeCategory = weapon.RangeCategory;
            rangeMonitor.Owner = Data.Owner;
            rangeMonitor.onEnemyInRange += OnEnemyInRange;
        }
        // rangeMonitors enable themselves

        Data.AddWeapon(weapon, rangeMonitor.ID);
        // IMPROVE how to keep track ranges from overlapping
    }

    /// <summary>
    /// Removes the weapon from this element, destroying any associated range tracker
    /// if it is no longer in use.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    public void RemoveWeapon(AWeapon weapon) {
        bool isRangeTrackerStillInUse = Data.RemoveWeapon(weapon);
        if (!isRangeTrackerStillInUse) {
            IWeaponRangeMonitor rangeTracker;
            if (_weaponRangeMonitorLookup.TryGetValue(weapon.MonitorID, out rangeTracker)) {
                _weaponRangeMonitorLookup.Remove(weapon.MonitorID);
                D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(IWeaponRangeMonitor).Name, weapon.Name);
                GameObject.Destroy((rangeTracker as Component).gameObject);
                return;
            }
            D.Error("{0} could not find {1} for {2}.", FullName, typeof(IWeaponRangeMonitor).Name, weapon.Name);
        }
    }

    #region Weapon Reload System

    private IDictionary<Guid, Job> _weaponReloadJobs = new Dictionary<Guid, Job>();

    private void OnEnemyInRange(bool isInRange, Guid monitorID) {
        D.Log("{0}.OnEnemyInRange(isInRange: {1}, monitorID: {2}).", FullName, isInRange, monitorID);
        var weapons = Data.GetWeapons(monitorID);
        foreach (var weapon in weapons) {
            var weaponID = weapon.ID;
            Job weaponReloadJob;
            if (isInRange) {
                if (!_weaponReloadJobs.TryGetValue(weaponID, out weaponReloadJob)) {
                    D.Log("{0} creating new weaponReloadJob for {1}.", FullName, weapon.Name);
                    weaponReloadJob = new Job(ReloadWeapon(weapon));
                    _weaponReloadJobs.Add(weaponID, weaponReloadJob);
                }
                D.Assert(!weaponReloadJob.IsRunning, "{0}.{1}.WeaponReloadJob should not be running.".Inject(FullName, weapon.Name));
                weaponReloadJob.Start();
            }
            else {
                weaponReloadJob = _weaponReloadJobs[weaponID];
                if (!weaponReloadJob.IsRunning) {
                    D.Warn("{0}.{1}.WeaponReloadJob should be running.".Inject(FullName, weapon.Name));
                }
                weaponReloadJob.Kill();
            }
        }
    }

    private IEnumerator ReloadWeapon(AWeapon weapon) {
        while (true) {
            OnWeaponReady(weapon);
            yield return new WaitForSeconds(weapon.ReloadPeriod);
        }
    }

    #endregion

    #endregion

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected override void OnDeath() {
        base.OnDeath();
        collider.enabled = false;
        _weaponRangeMonitorLookup.Values.ForAll(rt => rt.onEnemyInRange -= OnEnemyInRange);
        if (_weaponReloadJobs.Count != Constants.Zero) {
            _weaponReloadJobs.ForAll<KeyValuePair<Guid, Job>>(kvp => kvp.Value.Kill());
        }
    }

    #endregion

    # region StateMachine Callbacks

    public override void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnDetectedEnemy() {  //TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    /// <summary>
    /// Called when this weapon is ready to fire on a target in range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    void OnWeaponReady(AWeapon weapon) {
        RelayToCurrentState(weapon);
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IModel Members

    public override string FullName {
        get {
            if (IsHQElement) {
                return base.FullName + " [HQ]";
            }
            return base.FullName;
        }
    }

    #endregion

    #region IElementModel Members

    public event Action<ICmdModel> onCommandChanged;

    #endregion

}

