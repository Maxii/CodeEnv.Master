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
public abstract class AUnitElementModel : AMortalItemModelStateMachine, IUnitElement {

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    protected Rigidbody _rigidbody;
    protected IDictionary<Guid, IRangeTracker> _weaponRangeTrackerLookup;
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
        InitializeWeaponRangeTrackers();
    }

    private void InitializeWeaponRangeTrackers() {
        _weaponRangeTrackerLookup = new Dictionary<Guid, IRangeTracker>();
        var rangeTrackers = gameObject.GetSafeInterfacesInChildren<IRangeTracker>();

        foreach (var rangeTracker in rangeTrackers) {
            D.Assert(rangeTracker.Range != Constants.ZeroF, "{0} has an extra {1}.".Inject(Data.Name, typeof(IRangeTracker).Name));
            rangeTracker.Data = Data;
            rangeTracker.Owner = Data.Owner;
            rangeTracker.onEnemyInRange += OnEnemyInRange;
            _weaponRangeTrackerLookup.Add(rangeTracker.ID, rangeTracker);
        }
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        //TODO: Weapon values don't change but weapons do
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

    #region Weapon Reload System

    private IDictionary<Guid, Job> _weaponReloadJobs = new Dictionary<Guid, Job>();

    private void OnEnemyInRange(bool isInRange, Guid trackerID) {
        var weapons = Data.GetWeapons(trackerID);
        foreach (var weapon in weapons) {
            var weaponID = weapon.ID;
            Job weaponReloadJob;
            if (isInRange) {
                if (!_weaponReloadJobs.TryGetValue(weaponID, out weaponReloadJob)) {
                    weaponReloadJob = new Job(ReloadWeapon(weapon));
                    _weaponReloadJobs.Add(weaponID, weaponReloadJob);
                }
                D.Assert(!weaponReloadJob.IsRunning, "{0}.WeaponReloadJob should not be running.".Inject(Data.Name));
                weaponReloadJob.Start();
            }
            else {
                weaponReloadJob = _weaponReloadJobs[weaponID];
                D.Assert(weaponReloadJob.IsRunning, "{0}.ReloadWeaponJob should be running.".Inject(Data.Name));
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

    protected override void Cleanup() {
        base.Cleanup();
        _weaponRangeTrackerLookup.Values.ForAll(rt => (rt as IDisposable).Dispose());
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IUnitTarget Members

    public float MaxWeaponsRange { get { return Data.MaxWeaponsRange; } }

    #endregion
}

