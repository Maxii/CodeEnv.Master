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

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, AElement>(sb => sb.HQElement, OnHQElementChanged));
        _subscribers.Add(Item.Data.SubscribeToPropertyChanged<StarbaseData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, StarbaseState>(sb => sb.CurrentState, OnStarbaseStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
        Item.onElementDestroyed += OnStarbaseElementDestroyed;
    }

    private void OnStarbaseStateChanged() {
        StarbaseState state = Item.CurrentState;
        switch (state) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnStarbaseElementDestroyed(AElement element) {
        if (element.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our fleet's ship that was just destroyed was the focus, so change the focus to the fleet
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    public void __SimulateAllElementsAttacked() {
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

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    public void OnPlayerIntelChanged() {
        Item.Elements.ForAll<FacilityItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().PlayerIntel = View.PlayerIntel);
        AssessCmdIcon();
    }


    private void OnHQElementChanged() {
        View.TrackingTarget = GetHQElementTransform();
    }

    public void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
        Item.Elements.ForAll(s => s.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().AssessHighlighting());
    }

    public Transform GetHQElementTransform() {
        return Item.HQElement.transform;
    }

    private IconFactory _iconFactory = IconFactory.Instance;
    private void AssessCmdIcon() {
        IIcon icon;
        GameColor color = GameColor.White;
        // TODO evaluate Composition
        switch (View.PlayerIntel.Scope) {
            case IntelScope.None:
                icon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.None);
                //color = GameColor.Clear;    // None should be a completely transparent icon
                break;
            case IntelScope.Aware:
                icon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.IntelLevelUnknown);
                // color = GameColor.White;    // may be clear from prior setting
                break;
            case IntelScope.Minimal:
            case IntelScope.Moderate:
                icon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Level5);
                color = Item.Data.Owner.Color;
                break;
            case IntelScope.Comprehensive:
                var selectionCriteria = new IconSelectionCriteria[] { IconSelectionCriteria.Level5, IconSelectionCriteria.Science, IconSelectionCriteria.Colony, IconSelectionCriteria.Troop };
                icon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, selectionCriteria);
                color = Item.Data.Owner.Color;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(View.PlayerIntel.Scope));
        }
        D.Log("IntelScope is {2}, changing {0} to {1}.", typeof(FleetIcon).Name, icon.Filename, View.PlayerIntel.Scope.GetName());
        View.ChangeCmdIcon(icon, color);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

