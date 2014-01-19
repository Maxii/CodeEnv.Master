// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasePresenter.cs
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
public class StarbasePresenter : AMortalFocusablePresenter {

    public new StarbaseItem Item {
        get { return base.Item as StarbaseItem; }
        protected set { base.Item = value; }
    }

    //protected new IStarbaseViewable View {
    //    get { return base.View as IStarbaseViewable; }
    //}

    //public StarbasePresenter(IStarbaseViewable view)
    //    : base(view) {
    //    Subscribe();
    //}

    protected new ICommandViewable View {
        get { return base.View as ICommandViewable; }
    }

    public StarbasePresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }


    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<StarbaseItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<StarbaseData>(Item.Data);
    }

    //protected override void Subscribe() {
    //    base.Subscribe();
    //    _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, FacilityItem>(f => f.Flagship, OnFlagshipChanged));
    //    _subscribers.Add(Item.Data.SubscribeToPropertyChanged<StarbaseData, StarbaseComposition>(fd => fd.Composition, OnFleetCompositionChanged));
    //    _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, StarbaseState>(f => f.CurrentState, OnFleetStateChanged));
    //    View.onShowCompletion += Item.OnShowCompletion;
    //    Item.onFleetElementDestroyed += OnFleetElementDestroyed;
    //}

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, FacilityItem>(f => f.HQElement, OnFlagshipChanged));
        _subscribers.Add(Item.Data.SubscribeToPropertyChanged<StarbaseData, StarbaseComposition>(fd => fd.Composition, OnFleetCompositionChanged));
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, StarbaseState>(f => f.CurrentState, OnFleetStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
        Item.onElementDestroyed += OnFleetElementDestroyed;
    }


    private void OnFleetStateChanged() {
        StarbaseState fleetState = Item.CurrentState;
        switch (fleetState) {
            case StarbaseState.ShowDying:
                View.ShowDying();
                break;
            case StarbaseState.Dead:
            case StarbaseState.Dying:
            case StarbaseState.Idling:
            case StarbaseState.ProcessOrders:
                // do nothing
                break;
            case StarbaseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(fleetState));
        }
    }

    private void OnFleetElementDestroyed(FacilityItem ship) {
        if (ship.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our fleet's ship that was just destroyed was the focus, so change the focus to the fleet
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    //public void __SimulateAllShipsAttacked() {
    //    Item.Ships.ForAll<FacilityItem>(s => s.__SimulateAttacked());
    //}

    public void __SimulateAllShipsAttacked() {
        Item.Elements.ForAll<FacilityItem>(s => s.__SimulateAttacked());
    }


    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as StarbaseItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    private void OnFleetCompositionChanged() {
        AssessFleetIcon();
    }

    //public void NotifyShipsOfIntelChange() {
    //    Item.Ships.ForAll<FacilityItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().PlayerIntel = View.PlayerIntel);
    //    AssessFleetIcon();
    //}

    public void NotifyShipsOfIntelChange() {
        Item.Elements.ForAll<FacilityItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().PlayerIntel = View.PlayerIntel);
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
        Item.Elements.ForAll(s => s.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().AssessHighlighting());
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

