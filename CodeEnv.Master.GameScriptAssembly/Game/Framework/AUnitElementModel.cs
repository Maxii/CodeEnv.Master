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
    protected IRangeTracker _inWeaponRangeTargetTracker;


    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _inWeaponRangeTargetTracker = gameObject.GetSafeInterfaceInChildren<IRangeTracker>();
        UpdateRate = FrameUpdateFrequency.Rare;   // temp for fire rate
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Initialize() {
        _rigidbody.mass = Data.Mass;
        InitializeWeaponRangeTargetTrackers();
    }

    private void InitializeWeaponRangeTargetTrackers() {
        _inWeaponRangeTargetTracker.Data = Data;
        _inWeaponRangeTargetTracker.Range = Data.WeaponRange;
        _inWeaponRangeTargetTracker.Owner = Data.Owner;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        OnWeaponReady();
    }


    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AElementData, float>(d => d.WeaponRange, OnWeaponsRangeChanged));
    }

    private void OnWeaponsRangeChanged() {
        _inWeaponRangeTargetTracker.Range = Data.WeaponRange;
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        _inWeaponRangeTargetTracker.Owner = Data.Owner;
    }

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    #endregion

    # region StateMachine Callbacks

    public override void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    void OnWeaponReady() {
        RelayToCurrentState();
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    protected override void Cleanup() {
        base.Cleanup();
        (_inWeaponRangeTargetTracker as IDisposable).Dispose();
    }

}

