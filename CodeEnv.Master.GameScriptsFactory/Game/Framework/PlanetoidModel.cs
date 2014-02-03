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

using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all planetoids in the game.
/// </summary>
public class PlanetoidModel : AMortalItemModelStateMachine {

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Die() {
        base.Die();
        CurrentState = PlanetoidState.Dying;
    }

    #region Planetoid StateMachine

    private PlanetoidState _currentState;
    public new PlanetoidState CurrentState {
        get { return _currentState; }
        set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        base.CurrentState = _currentState;
    }

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    #endregion

    #region TakingDamage

    private float _hitDamage;

    void TakingDamage_EnterState() {
        Data.CurrentHitPoints -= _hitDamage;
        _hitDamage = 0F;
        Call(PlanetoidState.ShowHit);
        Return();   // returns to the state we were in when the OnHit event arrived
    }

    // TakingDamage is a transition state so _OnHit cannot occur here

    #endregion

    #region ShowHit

    void ShowHit_OnHit(float damage) {
        // View can not 'queue' show animations so just apply the damage
        // and wait for ShowXXX_OnCompletion to return to caller
        Data.CurrentHitPoints -= damage;
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
        // View is showing Dying
    }

    void ShowDying_OnShowCompletion() {
        Return();
    }

    #endregion

    #region Dead

    IEnumerator Dead_EnterState() {
        D.Log("{0} is Dead!", Data.Name);
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    # region Callbacks

    public void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnHit(float damage) {
        RelayToCurrentState(damage);    // IMPROVE add Action delegate to RelayToCurrentState
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

