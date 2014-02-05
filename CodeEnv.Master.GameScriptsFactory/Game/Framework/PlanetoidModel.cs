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

    protected override void NotifyOfDeath() {
        base.NotifyOfDeath();
        CurrentState = PlanetoidState.Dying;
    }

    #region StateMachine

    public new PlanetoidState CurrentState {
        get { return (PlanetoidState)base.CurrentState; }
        set { base.CurrentState = value; }
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnHit() {
        Call(PlanetoidState.TakingDamage);
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region TakingDamage

    void TakingDamage_EnterState() {
        LogEvent();
        bool isElementAlive = ApplyDamage();
        if (isElementAlive) {
            Call(PlanetoidState.ShowHit);
            Return();   // returns to the state we were in when the OnHit event arrived
        }
        else {
            CurrentState = PlanetoidState.Dying;
        }
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region ShowHit

    void ShowHit_EnterState() {
        OnStartShow();
    }

    void ShowHit_OnHit() {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        ApplyDamage();
    }

    void ShowHit_OnShowCompletion() {
        // View is showing Hit
        Return();
    }

    #endregion

    #region Dying

    void Dying_EnterState() {
        Call(PlanetoidState.ShowDying);
        CurrentState = PlanetoidState.Dead;
    }

    #endregion

    #region ShowDying

    void ShowDying_EnterState() {
        OnStartShow();
        // View is showing Dying
    }

    void ShowDying_OnShowCompletion() {
        Return();
    }

    #endregion

    #region Dead

    IEnumerator Dead_EnterState() {
        LogEvent();
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    #region StateMachine Support Methods

    private float _hitDamage;
    /// <summary>
    /// Applies the damage to the Element. Returns true 
    /// if the Element survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Element survived.</returns>
    protected bool ApplyDamage() {
        bool isAlive = true;
        Data.CurrentHitPoints -= _hitDamage;
        if (Data.Health <= Constants.ZeroF) {
            isAlive = false;
        }
        _hitDamage = Constants.ZeroF;
        return isAlive;
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
        _hitDamage = damage;
        OnHit();
    }

    void OnHit() {
        RelayToCurrentState();
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

