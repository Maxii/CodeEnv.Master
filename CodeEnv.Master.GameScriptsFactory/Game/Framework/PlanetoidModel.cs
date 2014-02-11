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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all planetoids in the game.
/// </summary>
public class PlanetoidModel : AMortalItemModelStateMachine {

    public event Action onStartShow;

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    #region StateMachine

    public new PlanetoidState CurrentState {
        get { return (PlanetoidState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Normal

    void Normal_EnterState() {
        // TODO register as available
    }

    void Normal_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region ShowHit

    void ShowHit_EnterState() {
        OnStartShow();
    }

    void ShowHit_OnShowCompletion() {
        // View is showing Hit
        Return();
    }

    #endregion

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnItemDeath();
        OnStartShow();
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        StartCoroutine(DelayedDestroy(3));
    }
    #endregion

    #region StateMachine Support Methods

    private IEnumerator DelayedDestroy(float delayInSeconds) {
        D.Log("{0}.DelayedDestroy({1}).", Data.Name, delayInSeconds);
        yield return new WaitForSeconds(delayInSeconds);
        D.Log("{0} GameObject being destroyed.", Data.Name);
        Destroy(gameObject);
    }

    private void OnStartShow() {
        var temp = onStartShow;
        if (temp != null) {
            onStartShow();
        }
    }

    #endregion

    # region StateMachine Callbacks

    public void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnHit(float damage) {
        if (CurrentState == PlanetoidState.Dead) {
            return;
        }
        Data.CurrentHitPoints -= damage;
        if (Data.Health > Constants.ZeroF) {
            CurrentState = PlanetoidState.Dead;
            return;
        }
        if (CurrentState == PlanetoidState.ShowHit) {
            // View can not 'queue' show animations so don't interrupt what is showing with another like show
            return;
        }
        Call(ShipState.ShowHit);
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

