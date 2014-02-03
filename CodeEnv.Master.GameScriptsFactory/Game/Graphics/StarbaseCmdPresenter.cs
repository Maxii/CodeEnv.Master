// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdPresenter.cs
// An MVPresenter associated with a StarbaseView.
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
/// An MVPresenter associated with a StarbaseView.
/// </summary>
public class StarbaseCmdPresenter : AUnitCommandPresenter<FacilityModel> {

    public new StarbaseCmdModel Model {
        get { return base.Model as StarbaseCmdModel; }
        protected set { base.Model = value; }
    }

    public StarbaseCmdPresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<StarbaseCmdModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<StarbaseData>(Model.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.Data.SubscribeToPropertyChanged<StarbaseData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
        _subscribers.Add(Model.SubscribeToPropertyChanged<StarbaseCmdModel, StarbaseState>(sb => sb.CurrentState, OnStarbaseStateChanged));
    }

    private void OnStarbaseStateChanged() {
        StarbaseState state = Model.CurrentState;
        switch (state) {
            case StarbaseState.ShowHit:
                View.ShowHit();
                break;
            case StarbaseState.ShowDying:
                View.ShowDying();
                break;
            case StarbaseState.Idling:
            case StarbaseState.ProcessOrders:
            case StarbaseState.GoAttack:
            case StarbaseState.Attacking:
            case StarbaseState.TakingDamage:
            case StarbaseState.GoRepair:
            case StarbaseState.Repairing:
            case StarbaseState.GoRefit:
            case StarbaseState.Refitting:
            case StarbaseState.GoDisband:
            case StarbaseState.Disbanding:
            case StarbaseState.Dying:
            case StarbaseState.Dead:
                // do nothing
                break;
            case StarbaseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

