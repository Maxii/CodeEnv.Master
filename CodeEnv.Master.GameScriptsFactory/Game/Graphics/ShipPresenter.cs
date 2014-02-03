﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPresenter.cs
// An MVPresenter associated with a ShipView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a ShipView.
/// </summary>
public class ShipPresenter : AUnitElementPresenter {

    public new ShipModel Model {
        get { return base.Model as ShipModel; }
        protected set { base.Model = value; }
    }

    public ShipPresenter(IElementViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<ShipModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<ShipData>(Model.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        return hudPublisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.SubscribeToPropertyChanging<ShipModel, ShipState>(s => s.CurrentState, OnShipStateChanging));
        _subscribers.Add(Model.SubscribeToPropertyChanged<ShipModel, ShipState>(s => s.CurrentState, OnShipStateChanged));
    }

    private void OnShipStateChanging(ShipState newState) {
        ShipState previousState = Model.CurrentState;
        switch (previousState) {
            case ShipState.Refitting:
            case ShipState.Repairing:
                // the state is changing from one of these states so stop the Showing
                View.StopShowing();
                break;
            case ShipState.ShowAttacking:
            case ShipState.ShowHit:
            case ShipState.ShowDying:
                // no need to stop any of these showing as they have already completed
                break;
            case ShipState.Entrenching:
            case ShipState.ProcessOrders:
            case ShipState.MovingTo:
            case ShipState.Idling:
            case ShipState.GoAttack:
            case ShipState.Dead:
            case ShipState.Chasing:
            case ShipState.Attacking:
            case ShipState.Dying:
            case ShipState.Joining:
            case ShipState.TakingDamage:
            case ShipState.Withdrawing:
                // do nothing
                break;
            case ShipState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousState));
        }
    }

    private void OnShipStateChanged() {
        ShipState newState = Model.CurrentState;
        switch (newState) {
            case ShipState.ShowAttacking:
                View.ShowAttacking();
                break;
            case ShipState.ShowHit:
                View.ShowHit();
                break;
            case ShipState.ShowDying:
                View.ShowDying();
                break;
            case ShipState.Entrenching:
                //View.ShowEntrenching();   // no current plans to show entrenching animation at this stage
                break;
            case ShipState.Refitting:
                View.ShowRefitting();
                break;
            case ShipState.Repairing:
                View.ShowRepairing();
                break;
            case ShipState.ProcessOrders:
            case ShipState.MovingTo:
            case ShipState.Idling:
            case ShipState.GoAttack:
            case ShipState.Dead:
            case ShipState.Chasing:
            case ShipState.Attacking:
            case ShipState.Dying:
            case ShipState.Joining:
            case ShipState.TakingDamage:
            case ShipState.Withdrawing:
                // do nothing
                break;
            case ShipState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }

    }

    public bool IsHQElement {
        get { return Model.IsHQElement; }
    }

    public Reference<float> GetShipSpeedReference() {
        return new Reference<float>(() => Model.Data.CurrentSpeed);
    }

    public void OnIsSelected() {
        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    }

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Model.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

