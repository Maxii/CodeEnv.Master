// --------------------------------------------------------------------------------------------------------------------
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
using UnityEngine;

/// <summary>
///  An MVPresenter associated with a Settlement View.
/// </summary>
public class SettlementPresenter : AMortalFocusablePresenter {

    public new SettlementItem Item {
        get { return base.Item as SettlementItem; }
        protected set { base.Item = value; }
    }

    protected new ICommandViewable View {
        get { return base.View as ICommandViewable; }
    }

    public SettlementPresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SettlementItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SettlementData>(Item.Data);
    }

    //protected override void Subscribe() {
    //    base.Subscribe();
    //    _subscribers.Add(Item.SubscribeToPropertyChanged<SettlementItem, SettlementState>(sb => sb.CurrentState, OnSettlementStateChanged));
    //    View.onShowCompletion += Item.OnShowCompletion;
    //}

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<SettlementItem, AElement>(sb => sb.HQElement, OnHQElementChanged));
        _subscribers.Add(Item.Data.SubscribeToPropertyChanged<SettlementData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
        _subscribers.Add(Item.SubscribeToPropertyChanged<SettlementItem, SettlementState>(sb => sb.CurrentState, OnSettlementStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
        Item.onElementDestroyed += OnSettlementElementDestroyed;
    }


    //private void OnSettlementStateChanged() {
    //    SettlementState state = Item.CurrentState;
    //    switch (state) {
    //        case SettlementState.ShowDying:
    //            View.ShowDying();
    //            break;
    //        case SettlementState.Idling:
    //            // do nothing
    //            break;
    //        case SettlementState.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
    //    }
    //}

    private void OnSettlementStateChanged() {
        SettlementState state = Item.CurrentState;
        switch (state) {
            case SettlementState.ShowDying:
                View.ShowDying();
                break;
            case SettlementState.Dead:
            case SettlementState.Dying:
            case SettlementState.Idling:
            case SettlementState.ProcessOrders:
                // do nothing
                break;
            case SettlementState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnSettlementElementDestroyed(AElement element) {
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

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    public void OnPlayerIntelChanged() {    // IMPROVE duplicates System communication of changes to all views in System
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

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as SettlementItem) == Item) {
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

