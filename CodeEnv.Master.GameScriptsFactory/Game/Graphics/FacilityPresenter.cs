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
        Model.onStopShow += OnStopShowInView;
        Model.onStartShow += OnStartShowInView;
    }

    private void OnStartShowInView() {
        FacilityState newState = Model.CurrentState;
        //D.Log("{0}.OnStartShowInView state = {1}.", Model.Data.Name, newState.GetName());
        switch (newState) {
            case FacilityState.ShowAttacking:
                View.ShowAttacking();
                break;
            case FacilityState.ShowHit:
                View.ShowHit();
                break;
            case FacilityState.ShowCmdHit:
                View.ShowCmdHit();
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

    private void OnStopShowInView() {
        FacilityState state = Model.CurrentState;
        switch (state) {
            case FacilityState.Refitting:
            case FacilityState.Repairing:
                View.StopShowing();
                break;
            case FacilityState.ShowAttacking:
            case FacilityState.ShowHit:
            case FacilityState.ShowCmdHit:
            case FacilityState.ShowDying:
                // no need to stop any of these showing as they complete at their own pace
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

