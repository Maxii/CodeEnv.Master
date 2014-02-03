// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityPresenter.cs
// An MVPresenter associated with a Facility View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a Facility View.
/// </summary>
public class FacilityPresenter : AUnitElementPresenter {

    public new FacilityModel Model {
        get { return base.Model as FacilityModel; }
        protected set { base.Model = value; }
    }

    public FacilityPresenter(IElementViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FacilityModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<FacilityData>(Model.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.SubscribeToPropertyChanging<FacilityModel, FacilityState>(sb => sb.CurrentState, OnFacilityStateChanging));
        _subscribers.Add(Model.SubscribeToPropertyChanged<FacilityModel, FacilityState>(sb => sb.CurrentState, OnFacilityStateChanged));
    }

    private void OnFacilityStateChanging(FacilityState newState) {
        FacilityState previousState = Model.CurrentState;
        switch (previousState) {
            case FacilityState.Refitting:
            case FacilityState.Repairing:
                // the state is changing from one of these states so stop the Showing
                View.StopShowing();
                break;
            case FacilityState.ShowAttacking:
            case FacilityState.ShowHit:
            case FacilityState.ShowDying:
                // no need to stop any of these showing as they have already completed
                break;
            case FacilityState.ProcessOrders:
            case FacilityState.Idling:
            case FacilityState.GoAttack:
            case FacilityState.Dead:
            case FacilityState.Attacking:
            case FacilityState.Dying:
            case FacilityState.TakingDamage:
                // do nothing
                break;
            case FacilityState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousState));
        }
    }

    private void OnFacilityStateChanged() {
        FacilityState newState = Model.CurrentState;
        switch (newState) {
            case FacilityState.ShowAttacking:
                View.ShowAttacking();
                break;
            case FacilityState.ShowHit:
                View.ShowHit();
                break;
            case FacilityState.ShowDying:
                View.ShowDying();
                break;
            case FacilityState.Refitting:
                View.ShowRefitting();
                break;
            case FacilityState.Repairing:
                View.ShowRepairing();
                break;
            case FacilityState.ProcessOrders:
            case FacilityState.Idling:
            case FacilityState.GoAttack:
            case FacilityState.Dead:
            case FacilityState.Attacking:
            case FacilityState.Dying:
            case FacilityState.TakingDamage:
                // do nothing
                break;
            case FacilityState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

