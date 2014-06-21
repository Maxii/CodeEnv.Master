// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdModel.cs
// The data-holding class for all Starbases in the game. Includes a state machine. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The data-holding class for all Starbases in the game. Includes a state machine. 
/// </summary>
public class StarbaseCmdModel : AUnitCommandModel, IStarbaseCmdModel, IBaseCmdTarget, IOrbitable {

    private BaseOrder<StarbaseDirective> _currentOrder;
    public BaseOrder<StarbaseDirective> CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder<StarbaseDirective>>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        base.InitializeRadiiComponents();
    }

    protected override void Initialize() {
        base.Initialize();
        CurrentState = StarbaseState.None;
        //D.Log("{0}.{1} Initialization complete.", FullName, GetType().Name);
    }

    public void CommenceOperations() {
        CurrentState = StarbaseState.Idling;
    }

    public override void AddElement(IElementModel element) {
        base.AddElement(element);

        IFacilityModel facility = element as IFacilityModel;
        // A facility that is in Idle without being part of a unit might attempt something it is not yet prepared for
        D.Assert(facility.CurrentState != FacilityState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, facility.FullName));

        facility.Command = this;
        if (HQElement != null) {
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    private void OnCurrentOrderChanged() {
        if (CurrentState == StarbaseState.Attacking) {
            Return();
        }
        if (CurrentOrder != null) {
            D.Log("{0} received new order {1}.", FullName, CurrentOrder.Directive.GetName());
            StarbaseDirective order = CurrentOrder.Directive;
            switch (order) {
                case StarbaseDirective.Attack:
                    CurrentState = StarbaseState.ExecuteAttackOrder;
                    break;
                case StarbaseDirective.StopAttack:

                    break;
                case StarbaseDirective.Repair:

                    break;
                case StarbaseDirective.Refit:

                    break;
                case StarbaseDirective.Disband:

                    break;
                case StarbaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(order));
            }
        }
    }

    protected override void PositionElementInFormation(IElementModel element, Vector3 stationOffset) {
        base.PositionElementInFormation(element, stationOffset);
        // set visitor orbit distance just outside of 'orbiting' facilities
        OrbitDistance = (element != HQElement) ? stationOffset.magnitude + 1F : 2F;   // base HQElement offset is always Vector3.zero     // IMPROVE  
    }

    protected override void KillCommand() {
        CurrentState = StarbaseState.Dead;
    }

    #region StateMachine

    public new StarbaseState CurrentState {
        get { return (StarbaseState)base.CurrentState; }
        protected set { base.CurrentState = value; }
    }

    #region None

    void None_EnterState() {
        //LogEvent();
    }

    void None_ExitState() {
        LogEvent();
        IsOperational = true;
    }

    #endregion

    #region Idle

    void Idling_EnterState() {
        //LogEvent();
        // register as available
    }

    void Idling_OnDetectedEnemy() { }

    void Idling_ExitState() {
        //LogEvent();
        // register as unavailable
    }

    #endregion

    #region ExecuteAttackOrder

    IEnumerator ExecuteAttackOrder_EnterState() {
        //LogEvent();
        D.Log("{0}.ExecuteAttackOrder_EnterState called.", Data.Name);
        Call(StarbaseState.Attacking);
        yield return null;  // required immediately after Call() to avoid FSM bug
        CurrentState = StarbaseState.Idling;
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();
    }

    #endregion

    #region Attacking

    IMortalTarget _attackTarget;

    void Attacking_EnterState() {
        LogEvent();
        _attackTarget = CurrentOrder.Target as IMortalTarget;
        _attackTarget.onTargetDeath += OnTargetDeath;
        var elementAttackOrder = new FacilityOrder(FacilityDirective.Attack, OrderSource.UnitCommand, _attackTarget);
        Elements.ForAll(e => (e as FacilityModel).CurrentOrder = elementAttackOrder);
    }

    void Attacking_OnTargetDeath(IMortalTarget deadTarget) {
        LogEvent();
        D.Assert(_attackTarget == deadTarget, "{0}.target {1} is not dead target {2}.".Inject(FullName, _attackTarget.FullName, deadTarget.FullName));
        Return();
    }

    void Attacking_ExitState() {
        LogEvent();
        _attackTarget.onTargetDeath -= OnTargetDeath;
        _attackTarget = null;
    }

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

    #region Dead

    void Dead_EnterState() {
        LogEvent();
        OnDeath();
        OnShowAnimation(MortalAnimations.Dying);
    }

    void Dead_OnShowCompletion() {
        LogEvent();
        new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    #endregion

    #region StateMachine Support Methods


    #endregion

    # region StateMachine Callbacks

    #endregion

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IOrbitable Members

    public float OrbitDistance { get; private set; }

    public void AssumeOrbit(IShipModel ship) {
        var shipOrbit = gameObject.GetComponentInImmediateChildren<ShipOrbit>();
        if (shipOrbit == null) {
            UnitFactory.Instance.MakeShipOrbitInstance(gameObject, ship);
        }
        else {
            UnitFactory.Instance.AttachShipToShipOrbit(ship, ref shipOrbit);
        }
    }

    public void LeaveOrbit(IShipModel orbitingShip) {
        var shipOrbit = gameObject.GetComponentInImmediateChildren<ShipOrbit>();
        D.Assert(shipOrbit != null, "{0}.{1} is not present.".Inject(FullName, typeof(ShipOrbit).Name));
        var ship = shipOrbit.gameObject.GetSafeInterfacesInChildren<IShipModel>().Single(s => s == orbitingShip);
        var parentFleetTransform = ship.Command.Transform.parent;
        ship.Transform.parent = parentFleetTransform;
    }

    #endregion

}

