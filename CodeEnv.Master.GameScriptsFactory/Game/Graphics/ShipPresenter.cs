// --------------------------------------------------------------------------------------------------------------------
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
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health);
        return hudPublisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        Model.onStopShow += OnStopShowInView;
        Model.onStartShow += OnStartShowInView;
    }

    private void OnStartShowInView() {
        ShipState state = Model.CurrentState;
        //D.Log("{0}.OnStartShowInView state = {1}.", Model.Data.Name, newState.GetName());
        switch (state) {
            case ShipState.Attacking:
                View.ShowAttacking();
                break;
            case ShipState.ShowHit:
                View.ShowHit();
                break;
            case ShipState.ShowCmdHit:
                View.ShowCmdHit();
                break;
            case ShipState.Refitting:
                View.ShowRefitting();
                break;
            case ShipState.Repairing:
                View.ShowRepairing();
                break;
            case ShipState.Dead:
                View.ShowDying();
                break;
            case ShipState.Entrenching:
            case ShipState.MovingTo:
            case ShipState.Idling:
            case ShipState.GoAttack:
            case ShipState.Chasing:
            case ShipState.Joining:
            case ShipState.Withdrawing:
                // do nothing
                break;
            case ShipState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnStopShowInView() {
        ShipState state = Model.CurrentState;
        switch (state) {
            case ShipState.Refitting:
            case ShipState.Repairing:
                View.StopShowing();
                break;
            case ShipState.ShowHit:
            case ShipState.ShowCmdHit:
            case ShipState.Dead:
                // no need to stop any of these showing as they complete at their own pace
                break;
            case ShipState.Entrenching:
            case ShipState.MovingTo:
            case ShipState.Idling:
            case ShipState.GoAttack:
            case ShipState.Chasing:
            case ShipState.Attacking:
            case ShipState.Joining:
            case ShipState.Withdrawing:
                // do nothing
                break;
            case ShipState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
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

