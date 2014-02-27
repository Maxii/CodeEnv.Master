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
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for an Element, an object that is under the command of a CommandItem.
/// </summary>
public abstract class AUnitElementModel : AMortalItemModelStateMachine {

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    private Rigidbody _rigidbody;
    protected IRangeTracker _weaponTargetTracker;
    protected float _gameSpeedMultiplier;


    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _weaponTargetTracker = gameObject.GetSafeInterfaceInChildren<IRangeTracker>();
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    }

    protected override void Initialize() {
        _rigidbody.mass = Data.Mass;
        InitializeWeaponRangeTargetTrackers();
        OnWeaponReloadPeriodChanged();
    }

    private void InitializeWeaponRangeTargetTrackers() {
        _weaponTargetTracker.Data = Data;
        _weaponTargetTracker.Range = Data.WeaponRange;
        _weaponTargetTracker.Owner = Data.Owner;
        _weaponTargetTracker.onEnemyInRange += OnEnemyInRange;
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AElementData, float>(d => d.WeaponRange, OnWeaponsRangeChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AElementData, float>(d => d.WeaponReloadPeriod, OnWeaponReloadPeriodChanged));
    }

    private void OnGameSpeedChanged() {
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        OnWeaponReloadPeriodChanged();
    }

    private void OnWeaponsRangeChanged() {
        _weaponTargetTracker.Range = Data.WeaponRange;
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        _weaponTargetTracker.Owner = Data.Owner;
    }

    #region Weapon Reload System

    private void OnWeaponReloadPeriodChanged() {
        _weaponReloadPeriod = Data.WeaponReloadPeriod / (GameDate.HoursPerSecond * _gameSpeedMultiplier);
    }

    private Job _reloadWeaponJob;
    private void OnEnemyInRange(bool isInRange) {
        if (isInRange) {
            if (_reloadWeaponJob == null) {
                _reloadWeaponJob = new Job(ReloadWeapon());
            }
            D.Assert(!_reloadWeaponJob.IsRunning, "{0}.ReloadWeaponJob should not be running.".Inject(Data.Name));
            _reloadWeaponJob.Start();
        }
        else {
            D.Assert(_reloadWeaponJob.IsRunning, "{0}.ReloadWeaponJob should be running.".Inject(Data.Name));
            _reloadWeaponJob.Kill();
        }
    }

    private float _weaponReloadPeriod;
    private IEnumerator ReloadWeapon() {
        while (true) {
            OnWeaponReady();
            yield return new WaitForSeconds(_weaponReloadPeriod);
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
        _weaponTargetTracker.onEnemyInRange -= OnEnemyInRange;
        if (_reloadWeaponJob != null) {
            _reloadWeaponJob.Kill();
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

    protected bool _isWeaponReady;
    void OnWeaponReady() {
        _isWeaponReady = true;
        RelayToCurrentState();
    }

    #endregion

    protected override void Cleanup() {
        base.Cleanup();
        (_weaponTargetTracker as IDisposable).Dispose();
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

}

