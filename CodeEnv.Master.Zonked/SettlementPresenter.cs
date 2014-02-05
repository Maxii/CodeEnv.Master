﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementPresenter.cs
//  An MVPresenter associated with a Settlement View.
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

/// <summary>
///  An MVPresenter associated with a Settlement View.
/// </summary>
public class SettlementPresenter : AMortalItemPresenter {

    public new SettlementCmdModel Item {
        get { return base.Model as SettlementCmdModel; }
        protected set { base.Model = value; }
    }

    protected new ISettlementViewable View {
        get { return base.View as ISettlementViewable; }
    }

    public SettlementCmdPresenter(ISettlementViewable view)
        : base(view) { }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SettlementCmdModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SettlementCmdData>(Mode.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Mode.SubscribeToPropertyChanged<SettlementCommandModel, SettlementState>(sb => sb.CurrentState, OnSettlementStateChanged));
        View.onShowCompletion += Mode.OnShowCompletion;
    }

    private void OnSettlementStateChanged() {
        SettlementState state = Mode.CurrentState;
        switch (state) {
            case SettlementState.ShowDying:
                View.ShowDying();
                break;
            case SettlementState.Idling:
                // do nothing
                break;
            case SettlementState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    protected override void OnItemDeath(MortalItemDeathEvent e) {
        if ((e.Source as SettlementCmdModel) == Mode) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a settlement
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

