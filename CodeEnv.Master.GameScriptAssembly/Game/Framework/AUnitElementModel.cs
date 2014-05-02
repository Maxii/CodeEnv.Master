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
public abstract class AUnitElementModel : AMortalItemModelStateMachine, IElementModel, IElementTarget {

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    private ICommandModel _command;
    public ICommandModel Command {
        get { return _command; }
        set { SetProperty<ICommandModel>(ref _command, value, "Command"); }
    }

    protected Rigidbody _rigidbody;
    /// <summary>
    /// Weapon Range Tracker lookup table keyed by the Range Tracker's Guid ID.
    /// </summary>
    protected IDictionary<Guid, IWeaponRangeTracker> _weaponRangeTrackerLookup;
    protected float _gameSpeedMultiplier;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
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
            _weaponRangeTrackerLookup.Values.ForAll(rt => rt.Owner = Data.Owner);
        }
    }

    protected override void OnNamingChanged() {
        base.OnNamingChanged();
        //if (enabled) {  // UNCLEAR no longer needed?
        _weaponRangeTrackerLookup.Values.ForAll(rt => rt.ParentFullName = Data.FullName);
        //}
    }

    #region Weapons

    /// <summary>
    /// Adds the weapon to this element, paired with the provided range tracker. Clients wishing to add
    /// a weapon to this element should use UnitFactory.AddWeapon(weapon, element).
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="rangeTracker">The range tracker to pair with this weapon.</param>
    public void AddWeapon(Weapon weapon, IWeaponRangeTracker rangeTracker) {
        if (_weaponRangeTrackerLookup == null) {
            _weaponRangeTrackerLookup = new Dictionary<Guid, IWeaponRangeTracker>();
        }
        if (!_weaponRangeTrackerLookup.ContainsKey(rangeTracker.ID)) {
            // only need to record and setup range trackers once. The same rangeTracker can have more than 1 weapon
            _weaponRangeTrackerLookup.Add(rangeTracker.ID, rangeTracker);
            rangeTracker.Range = weapon.Range;
            rangeTracker.ParentFullName = FullName;
            rangeTracker.Owner = Data.Owner;
            rangeTracker.onEnemyInRange += OnEnemyInRange;
        }
        // rangeTrackers enable themselves

        Data.AddWeapon(weapon, rangeTracker.ID);
        // IMPROVE how to keep track ranges from overlapping
    }

    /// <summary>
    /// Removes the weapon from this element, destroying any associated range tracker
    /// if it is no longer in use.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    public void RemoveWeapon(Weapon weapon) {
        bool isRangeTrackerStillInUse = Data.RemoveWeapon(weapon);
        if (!isRangeTrackerStillInUse) {
            IWeaponRangeTracker rangeTracker;
            if (_weaponRangeTrackerLookup.TryGetValue(weapon.TrackerID, out rangeTracker)) {
                _weaponRangeTrackerLookup.Remove(weapon.TrackerID);
                D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(IWeaponRangeTracker).Name, weapon.Name);
                GameObject.Destroy((rangeTracker as Component).gameObject);
                return;
            }
            D.Error("{0} could not find {1} for {2}.", FullName, typeof(IWeaponRangeTracker).Name, weapon.Name);
        }
    }

    #region Weapon Reload System

    private IDictionary<Guid, Job> _weaponReloadJobs = new Dictionary<Guid, Job>();

    private void OnEnemyInRange(bool isInRange, Guid trackerID) {
        D.Log("{0}.OnEnemyInRange(isInRange: {1}, trackerID: {2}).", FullName, isInRange, trackerID);
        var weapons = Data.GetWeapons(trackerID);
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

    private IEnumerator ReloadWeapon(Weapon weapon) {
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

    protected override void OnItemDeath() {
        base.OnItemDeath();
        _weaponRangeTrackerLookup.Values.ForAll(rt => rt.onEnemyInRange -= OnEnemyInRange);
        if (_weaponReloadJobs.Count != Constants.Zero) {
            _weaponReloadJobs.ForAll<KeyValuePair<Guid, Job>>(kvp => kvp.Value.Kill());
        }
    }

    #endregion

    # region StateMachine Callbacks

    public override void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    /// <summary>
    /// Called when this weapon is ready to fire on a target in range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    void OnWeaponReady(Weapon weapon) {
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

}

