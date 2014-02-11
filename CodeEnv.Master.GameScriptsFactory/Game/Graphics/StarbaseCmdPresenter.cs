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
        var publisher = new GuiHudPublisher<StarbaseCmdData>(Model.Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.Data.SubscribeToPropertyChanged<StarbaseCmdData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
    }

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    protected override void OnStartShowInView() {
        StarbaseState state = Model.CurrentState;
        //D.Log("{0}.OnStartShowInView state = {1}.", Model.Data.Name, state.GetName());
        switch (state) {
            case StarbaseState.Dead:
                View.ShowDying();
                break;
            case StarbaseState.Attacking:
            case StarbaseState.Refitting:
            case StarbaseState.Repairing:
            case StarbaseState.Idling:
            case StarbaseState.GoAttack:
            case StarbaseState.Disbanding:
            case StarbaseState.GoDisband:
            case StarbaseState.GoRefit:
            case StarbaseState.GoRepair:
                // do nothing
                break;
            case StarbaseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

