// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetPresenter.cs
// An MVPresenter associated with a FleetView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a FleetView.
/// </summary>
public class FleetPresenter : AMortalFocusablePresenter {

    public new FleetItem Item {
        get { return base.Item as FleetItem; }
        protected set { base.Item = value; }
    }

    //protected new IFleetViewable View {
    //    get { return base.View as IFleetViewable; }
    //}

    //public FleetPresenter(IFleetViewable view)
    //    : base(view) {
    //    Subscribe();
    //}

    protected new ICommandViewable View {
        get { return base.View as ICommandViewable; }
    }

    public FleetPresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }


    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FleetItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<FleetData>(Item.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        return hudPublisher;
    }

    //protected override void Subscribe() {
    //    base.Subscribe();
    //    _subscribers.Add(Item.SubscribeToPropertyChanged<FleetItem, ShipItem>(f => f.Flagship, OnFlagshipChanged));
    //    _subscribers.Add(Item.Data.SubscribeToPropertyChanged<FleetData, FleetComposition>(fd => fd.Composition, OnFleetCompositionChanged));
    //    _subscribers.Add(Item.SubscribeToPropertyChanged<FleetItem, FleetState>(f => f.CurrentState, OnFleetStateChanged));
    //    View.onShowCompletion += Item.OnShowCompletion;
    //    Item.onFleetElementDestroyed += OnFleetElementDestroyed;
    //}

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<FleetItem, ShipItem>(f => f.HQElement, OnFlagshipChanged));
        _subscribers.Add(Item.Data.SubscribeToPropertyChanged<FleetData, FleetComposition>(fd => fd.Composition, OnFleetCompositionChanged));
        _subscribers.Add(Item.SubscribeToPropertyChanged<FleetItem, FleetState>(f => f.CurrentState, OnFleetStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
        Item.onElementDestroyed += OnFleetElementDestroyed;
    }


    private void OnFleetStateChanged() {
        FleetState fleetState = Item.CurrentState;
        switch (fleetState) {
            case FleetState.ShowDying:
                View.ShowDying();
                break;
            case FleetState.Attacking:
            case FleetState.Dead:
            case FleetState.Disbanding:
            case FleetState.Dying:
            case FleetState.Entrenching:
            case FleetState.GoAttack:
            case FleetState.GoDisband:
            case FleetState.GoGuard:
            case FleetState.GoJoin:
            case FleetState.GoPatrol:
            case FleetState.GoRefit:
            case FleetState.GoRepair:
            case FleetState.GoRetreat:
            case FleetState.Guarding:
            case FleetState.Idling:
            case FleetState.MovingTo:
            case FleetState.Patrolling:
            case FleetState.ProcessOrders:
            case FleetState.Refitting:
            case FleetState.Repairing:
                // do nothing
                break;
            case FleetState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(fleetState));
        }
    }

    private void OnFleetElementDestroyed(ShipItem ship) {
        if (ship.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our fleet's ship that was just destroyed was the focus, so change the focus to the fleet
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    //public void __SimulateAllShipsAttacked() {
    //    Item.Ships.ForAll<ShipItem>(s => s.__SimulateAttacked());
    //}

    public void __SimulateAllShipsAttacked() {
        Item.Elements.ForAll<ShipItem>(s => s.__SimulateAttacked());
    }


    public Reference<float> GetFleetSpeed() {
        return new Reference<float>(() => Item.Data.CurrentSpeed);
    }

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as FleetItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public void __RandomChangeOfHeadingAndSpeed() {
        Item.ChangeHeading(UnityEngine.Random.insideUnitSphere.normalized);
        Item.ChangeSpeed(UnityEngine.Random.Range(Constants.ZeroF, 2.5F));
    }

    private void OnFleetCompositionChanged() {
        AssessFleetIcon();
    }

    //public void NotifyShipsOfIntelChange() {
    //    Item.Ships.ForAll<ShipItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<ShipView>().PlayerIntel = View.PlayerIntel);
    //    AssessFleetIcon();
    //}

    public void NotifyShipsOfIntelChange() {
        Item.Elements.ForAll<ShipItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<ShipView>().PlayerIntel = View.PlayerIntel);
        AssessFleetIcon();
    }


    private void OnFlagshipChanged() {
        View.TrackingTarget = GetFlagship();
    }

    //public void OnIsSelectedChanged() {
    //    if ((View as ISelectable).IsSelected) {
    //        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    //    }
    //    Item.Ships.ForAll(s => s.gameObject.GetSafeMonoBehaviourComponent<ShipView>().AssessHighlighting());
    //}

    //public Transform GetFlagship() {
    //    return Item.Flagship.transform;
    //}

    public void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
        Item.Elements.ForAll(s => s.gameObject.GetSafeMonoBehaviourComponent<ShipView>().AssessHighlighting());
    }

    public Transform GetFlagship() {
        return Item.HQElement.transform;
    }


    private IconFactory _iconFactory = IconFactory.Instance;
    private void AssessFleetIcon() {
        IIcon fleetIcon;
        GameColor color = GameColor.White;
        // TODO evaluate Composition
        switch (View.PlayerIntel.Scope) {
            case IntelScope.None:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.None);
                //color = GameColor.Clear;    // None should be a completely transparent icon
                break;
            case IntelScope.Aware:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.IntelLevelUnknown);
                // color = GameColor.White;    // may be clear from prior setting
                break;
            case IntelScope.Minimal:
            case IntelScope.Moderate:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Level5);
                color = Item.Data.Owner.Color;
                break;
            case IntelScope.Comprehensive:
                var selectionCriteria = new IconSelectionCriteria[] { IconSelectionCriteria.Level5, IconSelectionCriteria.Science, IconSelectionCriteria.Colony, IconSelectionCriteria.Troop };
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, selectionCriteria);
                color = Item.Data.Owner.Color;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(View.PlayerIntel.Scope));
        }
        D.Log("IntelScope is {2}, changing {0} to {1}.", typeof(FleetIcon).Name, fleetIcon.Filename, View.PlayerIntel.Scope.GetName());
        View.ChangeFleetIcon(fleetIcon, color);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

