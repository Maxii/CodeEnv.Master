// --------------------------------------------------------------------------------------------------------------------
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
public class PlanetoidModel : AMortalItemModel {
    //public class PlanetoidModel : AMortalItemModelStateMachine {

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        CurrentState = PlanetoidState.Normal;
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

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        //D.Log("{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetName());
        switch (CurrentState) {
            case PlanetoidState.Normal:
                // do nothing
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
                StartCoroutine(DelayedDestroy(3));
                break;
            case PlanetoidState.Normal:
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

    //#region Normal

    //void Normal_EnterState() {
    //    // TODO register as available
    //}

    //void Normal_ExitState() {
    //    // TODO register as unavailable
    //}

    //#endregion

    //#region ShowHit

    //void ShowHit_EnterState() {
    //    OnStartShow();
    //}

    //void ShowHit_OnShowCompletion() {
    //    // View is showing Hit
    //    Return();
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

    #region ITarget Members

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

