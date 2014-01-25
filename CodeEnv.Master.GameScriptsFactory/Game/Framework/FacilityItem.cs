// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityItem.cs
// The data-holding class for all Facilities in the game. Includes a state machine.
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
/// The data-holding class for all Facilities in the game. Includes a state machine.
/// </summary>
public class FacilityItem : AElement {

    public new FacilityData Data {
        get { return base.Data as FacilityData; }
        set { base.Data = value; }
    }

    private FacilityState _currentState;
    public new FacilityState CurrentState {
        get { return _currentState; }
        set { SetProperty<FacilityState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        base.CurrentState = _currentState;
    }

    private ACommandItem<FacilityItem> _command;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        // when a Starbase or Settlement is initially built, the facility already selected to be the HQ assigns itself
        // to the command. As Command will immediately callback, Facility must do any
        // required initialization now, before the callback takes place
        var parent = _transform.parent;
        _command = parent.gameObject.GetSafeMonoBehaviourComponentInChildren<ACommandItem<FacilityItem>>();
        if (IsHQElement) {
            _command.HQElement = this;
        }
        CurrentState = FacilityState.Idling;
    }

    public void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            OnHit(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
        }
    }

    protected override void Die() {
        _command.ReportElementLost(this);
        // let Cmd process the loss before the destroyed facility starts processing its state changes
        CurrentState = FacilityState.Dying;
    }

    #region FacilityStates

    #region Idle

    void Idling_EnterState() {
        //D.Log("{0} Idling_EnterState", Data.Name);
        // TODO register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = FacilityState.ProcessOrders;
    }

    void Idling_ExitState() {
        // TODO register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region Attack

    void GoAttack_EnterState() { }

    void Attacking_EnterState() { }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Refit

    void GoRefit_EnterState() { }

    void Refitting_EnterState() { }

    #endregion

    #region Disband

    void GoDisband_EnterState() { }

    void Disbanding_EnterState() { }

    #endregion

    #region Dying

    void Dying_EnterState() {
        Call(FacilityState.ShowDying);
        CurrentState = FacilityState.Dead;
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
        D.Log("{0} has Died!", Data.Name);
        GameEventManager.Instance.Raise<ItemDeathEvent>(new ItemDeathEvent(this));
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    #endregion

    # region Callbacks

    public void OnShowCompletion() { RelayToCurrentState(); }

    void OnHit(float damage) {
        RelayToCurrentState(damage);    // IMPROVE add Action delegate to RelayToCurrentState
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ITarget Members

    public override bool IsMovable { get { return false; } }

    #endregion

}

