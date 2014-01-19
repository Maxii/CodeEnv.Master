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
public class FacilityItem : AElement<FacilityCategory, FacilityData, FacilityState> {
    //public class FacilityItem : AMortalItemStateMachine<FacilityState>, ITarget {

    //public bool IsFlagship { get; set; }

    //public new FacilityData Data {
    //    get { return base.Data as FacilityData; }
    //    set { base.Data = value; }
    //}

    //private ItemOrder<SettlementOrders> _currentOrder;
    //public ItemOrder<SettlementOrders> CurrentOrder {
    //    get { return _currentOrder; }
    //    set { SetProperty<ItemOrder<SettlementOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    //}

    //private GameManager _gameMgr;
    private StarbaseItem _fleet;

    protected override void Awake() {
        base.Awake();
        //_gameMgr = GameManager.Instance;
        Subscribe();
    }

    //protected override void Start() {
    //    base.Start();
    //    Initialize();
    //}

    //private void Initialize() {
    //    // when a Starbase is initially built, the facility already selected to be the flagship assigns itself
    //    // to Starbase command. As Starbase Command will immediately callback, Facility must do any
    //    // required initialization now, before the callback takes place
    //    var fleetParent = gameObject.GetSafeMonoBehaviourComponentInParents<StarbaseCreator>();
    //    _fleet = fleetParent.gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseItem>();
    //    if (IsFlagship) {
    //        _fleet.Flagship = this;
    //    }
    //    CurrentState = FacilityState.Idling;
    //}

    protected override void Initialize() {
        // when a Starbase is initially built, the facility already selected to be the HQ assigns itself
        // to Starbase command. As Starbase Command will immediately callback, Facility must do any
        // required initialization now, before the callback takes place
        var fleetParent = gameObject.GetSafeMonoBehaviourComponentInParents<StarbaseCreator>();
        _fleet = fleetParent.gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseItem>();
        if (IsHQElement) {
            //_fleet.Flagship = this;
            _fleet.HQElement = this;
        }
        CurrentState = FacilityState.Idling;
    }

    //protected override void Subscribe() {
    //    base.Subscribe();
    //    _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    //}

    //private void OnGameStateChanged() {
    //    // TODO
    //}

    //protected override void OnDataChanged() {
    //    base.OnDataChanged();
    //    rigidbody.mass = Data.Mass;
    //}

    public void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            OnHit(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
        }
    }

    protected override void Die() {
        _fleet.ReportElementLost(this);
        // let fleetCmd process the loss before the destroyed ship starts processing its state changes
        CurrentState = FacilityState.Dying;
    }


    //protected override void Die() {
    //    _fleet.ReportShipLost(this);
    //    // let fleetCmd process the loss before the destroyed ship starts processing its state changes
    //    CurrentState = FacilityState.Dying;
    //}

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

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

    //public string Name {
    //    get { return Data.Name; }
    //}

    //public Vector3 Position {
    //    get { return Data.Position; }
    //}

    //public bool IsMovable { get { return true; } }

    public override bool IsMovable { get { return false; } }

    #endregion

}

