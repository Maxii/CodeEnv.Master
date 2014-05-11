﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidModel.cs
// The data-holding class for all planetoids in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all planetoids in the game.
/// </summary>
public class PlanetoidModel : AMortalItemModel, IPlanetoidModel {
    //public class PlanetoidModel : AMortalItemModelStateMachine {

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        // IMPROVE planetoid colliders vary in radius. They are currently manually preset via the editor to match their mesh size in their prefab
        InitializeKeepoutCollider();
        Subscribe();
    }

    private void InitializeKeepoutCollider() {
        SphereCollider keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutCollider.radius = Radius * TempGameValues.KeepoutRadiusMultiplier;
    }

    protected override void Initialize() {
        base.Initialize();
        CurrentState = PlanetoidState.None;
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        PropogateOwnerChangeToMoons();
    }

    private void PropogateOwnerChangeToMoons() {
        var moons = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>().Except(this);
        if (!moons.IsNullOrEmpty()) {
            moons.ForAll(m => m.Data.Owner = Data.Owner);
        }
    }

    #region StateMachine - Simple Alternative

    // state machine is started by SystemCreator onIsRunning

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging); }
    }

    private void OnCurrentStateChanging(PlanetoidState newState) {
        PlanetoidState previousState = CurrentState;
        //D.Log("{0}.CurrentState changing from {1} to {2}.", Data.Name, previousState.GetName(), newState.GetName());
        switch (previousState) {
            case PlanetoidState.None:
                IsOperational = true;
                break;
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousState));
        }
    }

    private void OnCurrentStateChanged() {
        //D.Log("{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetName());
        switch (CurrentState) {
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
                OnItemDeath();
                OnShowAnimation(MortalAnimations.Dying);
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    public override void OnShowCompletion() {
        switch (CurrentState) {
            case PlanetoidState.Dead:
                new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
                    D.Log("{0} has been destroyed.", FullName);
                });
                break;
            case PlanetoidState.Idling:
                // do nothing
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    #endregion

    #region StateMachine - Full featured

    //public new PlanetoidState CurrentState {
    //    get { return (PlanetoidState)base.CurrentState; }
    //    set { base.CurrentState = value; }
    //}

    //#region None

    //void None_EnterState() {
    //    LogEvent();
    //}

    //void None_ExitState() {
    //    LogEvent();
    //    IsOperational = true;
    //}

    //#endregion

    //#region Idling

    //void Idling_EnterState() {
    //    // LogEvent();
    //}

    //void Idling_ExitState() {
    //LogEvent();
    //}

    //#endregion

    //#region Dead

    //void Dead_EnterState() {
    //    LogEvent();
    //    OnItemDeath();
    //    OnStartShow();
    //}

    //void Dead_OnShowCompletion() {
    //    LogEvent();
    //    StartCoroutine(DelayedDestroy(3));
    //}
    //#endregion

    //# region StateMachine Callbacks

    //public override void OnShowCompletion() {
    //    RelayToCurrentState();
    //}

    //protected override void OnHit(float damage) {
    //    if (CurrentState == PlanetoidState.Dead) {
    //        return;
    //    }
    //    Data.CurrentHitPoints -= damage;
    //    if (Data.Health > Constants.ZeroF) {
    //        CurrentState = PlanetoidState.Dead;
    //        return;
    //    }
    //    if (CurrentState == PlanetoidState.ShowHit) {
    //        // View can not 'queue' show animations so don't interrupt what is showing with another like show
    //        return;
    //    }
    //    Call(PlanetoidState.ShowHit);
    //}

    //#endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IMortalTarget Members

    public override void TakeDamage(float damage) {
        if (CurrentState == PlanetoidState.Dead) {
            return;
        }
        LogEvent();
        bool isAlive = ApplyDamage(damage);
        if (!isAlive) {
            CurrentState = PlanetoidState.Dead;
            return;
        }
        OnShowAnimation(MortalAnimations.Hit);
    }

    #endregion

}

