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
public class ShipPresenter : AMortalFocusablePresenter {

    public new ShipItem Item {
        get { return base.Item as ShipItem; }
        protected set { base.Item = value; }
    }

    protected new IShipViewable View {
        get { return base.View as IShipViewable; }
    }

    private IFleetViewable _fleetView;

    public ShipPresenter(IShipViewable view)
        : base(view) {
        FleetCreator fleetMgr = _viewGameObject.GetSafeMonoBehaviourComponentInParents<FleetCreator>();
        _fleetView = fleetMgr.gameObject.GetSafeInterfaceInChildren<IFleetViewable>();
        Subscribe();
    }

    protected override AItem InitilizeItemLinkage() {
        return UnityUtility.ValidateMonoBehaviourPresence<ShipItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<ShipData>(Item.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        return hudPublisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanging<ShipItem, ShipState>(s => s.CurrentState, OnShipStateChanging));
        View.onShowCompletion += Item.OnShowCompletion;
    }

    private void OnShipStateChanging(ShipState newState) {
        ShipState previousState = Item.CurrentState;
        switch (previousState) {
            case ShipState.Entrenching:
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
                View.ShowEntrenching();
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

    public bool IsFleetSelected {
        get { return (_fleetView as ISelectable).IsSelected; }
        set { (_fleetView as ISelectable).IsSelected = value; }
    }

    public void __SimulateAttacked() {
        Item.__SimulateAttacked();
    }

    public Reference<float> GetShipSpeed() {
        return new Reference<float>(() => Item.Data.CurrentSpeed);
    }

    public void OnIsSelected() {
        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    }

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as ShipItem) == Item) {
            CleanupOnDeath();
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

