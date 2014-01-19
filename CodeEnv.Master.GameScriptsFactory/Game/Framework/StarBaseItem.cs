// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseItem.cs
// The data-holding class for all Starbases in the game. Includes a state machine. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all Starbases in the game. Includes a state machine. 
/// </summary>
public class StarbaseItem : ACommandItem<FacilityItem, FacilityCategory, FacilityData, FacilityState, StarbaseData, StarbaseComposition, StarbaseState> {
    //public class StarbaseItem : AMortalItemStateMachine<StarbaseState>, ITarget {

    //public event Action<FacilityItem> onFleetElementDestroyed;

    //public string FleetName { get { return Data.OptionalParentName; } }

    private ItemOrder<StarbaseOrders> _currentOrder;
    public ItemOrder<StarbaseOrders> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<ItemOrder<StarbaseOrders>>(ref _currentOrder, value, "CurrentOrder", OnOrdersChanged); }
    }

    //public new StarbaseData Data {
    //    get { return base.Data as StarbaseData; }
    //    set { base.Data = value; }
    //}

    //private FacilityItem _flagship;
    //public FacilityItem Flagship {
    //    get { return _flagship; }
    //    set { SetProperty<FacilityItem>(ref _flagship, value, "Flagship", OnFlagshipChanged, OnFlagshipChanging); }
    //}

    //public IList<FacilityItem> Ships { get; private set; }

    protected override void Awake() {
        base.Awake();
        //Ships = new List<FacilityItem>();
        Subscribe();
    }

    //protected override void Start() {
    //    base.Start();
    //    Initialize();
    //}

    //private void Initialize() {
    //    CurrentState = StarbaseState.Idling;
    //}


    protected override void Initialize() {
        CurrentState = StarbaseState.Idling;
    }

    ///// <summary>
    ///// Adds the ship to this fleet including parenting if needed.
    ///// </summary>
    ///// <param name="ship">The ship.</param>
    //public void AddShip(FacilityItem ship) {
    //    Ships.Add(ship);
    //    Data.Add(ship.Data);
    //    Transform parentFleetTransform = gameObject.GetSafeMonoBehaviourComponentInParents<StarbaseCreator>().transform;
    //    if (ship.transform.parent != parentFleetTransform) {
    //        ship.transform.parent = parentFleetTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
    //    }
    //    // TODO consider changing flagship
    //}

    //public void ReportShipLost(FacilityItem ship) {
    //    D.Log("{0} acknowledging {1} has been lost.", Data.Name, ship.Data.Name);
    //    RemoveShip(ship);

    //    var fed = onFleetElementDestroyed;
    //    if (fed != null) {
    //        fed(ship);
    //    }
    //}

    //public void RemoveShip(FacilityItem ship) {
    //    bool isRemoved = Ships.Remove(ship);
    //    isRemoved = isRemoved && Data.Remove(ship.Data);
    //    D.Assert(isRemoved, "{0} not found.".Inject(ship.Data.Name));
    //    if (Ships.Count > Constants.Zero) {
    //        if (ship == Flagship) {
    //            // Flagship has died
    //            Flagship = SelectBestShip();
    //        }
    //        return;
    //    }
    //    // Fleet knows when to die
    //}

    //private void OnFlagshipChanging(FacilityItem newFlagship) {
    //    if (Flagship != null) {
    //        Flagship.IsFlagship = false;
    //    }
    //}

    //private void OnFlagshipChanged() {
    //    Flagship.IsFlagship = true;
    //    Data.HQItemData = Flagship.Data;
    //}

    protected override FacilityItem SelectHQElement() {
        return Elements.MaxBy<FacilityItem, float>(e => e.Radius);
    }

    protected override void Die() {
        CurrentState = StarbaseState.Dying;
    }

    //private FacilityItem SelectBestShip() {
    //    return Ships.MaxBy(s => s.Data.Health);
    //}

    //protected override void Cleanup() {
    //    base.Cleanup();
    //    Data.Dispose();
    //}

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region FleetStates

    #region Idle

    void Idling_EnterState() {
        //CurrentOrder = null;
        //if (Data.RequestedSpeed != Constants.ZeroF) {
        //    ChangeSpeed(Constants.ZeroF);
        //}
        // register as available
    }

    void Idling_OnOrdersChanged() {
        CurrentState = StarbaseState.ProcessOrders;
    }

    void Idling_ExitState() {
        // register as unavailable
    }

    void Idling_OnDetectedEnemy() { }


    #endregion

    #region ProcessOrders

    private ItemOrder<StarbaseOrders> _orderBeingExecuted;
    private bool _isNewOrderWaiting;

    void ProcessOrders_EnterState() { }

    void ProcessOrders_Update() {
        // I got to this state one of two ways:
        // 1. there has been a new order issued, or
        // 2. the last new order (_orderBeingExecuted) has been completed
        _isNewOrderWaiting = _orderBeingExecuted != CurrentOrder;
        if (_isNewOrderWaiting) {
            StarbaseOrders order = CurrentOrder.Order;
            switch (order) {
                case StarbaseOrders.Attack:

                    break;
                case StarbaseOrders.StopAttack:

                    break;
                case StarbaseOrders.Disband:

                    break;
                case StarbaseOrders.Repair:

                    break;
                case StarbaseOrders.Refit:

                    break;
                case StarbaseOrders.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
            _orderBeingExecuted = CurrentOrder;
        }
        else {
            // there is no new order so the return to this state must be after the last new order has been completed
            D.Assert(false, "Should be no Return() here.");
            CurrentState = StarbaseState.Idling;
        }
    }

    #endregion

    #region Attack

    void GoAttack_EnterState() { }

    void Attacking_EnterState() { }

    #endregion

    #region Repair

    void GoRepair_EnterState() { }

    void Repairing_EnterState() { }

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
        Call(StarbaseState.ShowDying);
        CurrentState = StarbaseState.Dead;
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

    void OnOrdersChanged() {
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", Data.Name, CurrentOrder.Order.GetName());
            RelayToCurrentState();
        }
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

