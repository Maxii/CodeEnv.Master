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
public class FleetPresenter : AMortalItemPresenter {

    public new FleetCmdModel Item {
        get { return base.Model as FleetCmdModel; }
        protected set { base.Model = value; }
    }

    protected new IFleetViewable View {
        get { return base.View as IFleetViewable; }
    }

    public FleetCmdPresenter(IFleetViewable view)
        : base(view) {
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FleetCmdModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var hudPublisher = new GuiHudPublisher<FleetCmdData>(Model.Data);
        hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        return hudPublisher;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.SubscribeToPropertyChanged<FleetCmdModel, ShipModel>(f => f.Flagship, OnFlagshipChanged));
        _subscribers.Add(Model.Data.SubscribeToPropertyChanged<FleetCmdData, FleetComposition>(fd => fd.Composition, OnFleetCompositionChanged));
        _subscribers.Add(Model.SubscribeToPropertyChanged<FleetCmdModel, FleetState>(f => f.CurrentState, OnFleetStateChanged));
        View.onShowCompletion += Model.OnShowCompletion;
        Model.onFleetElementDestroyed += OnFleetElementDestroyed;
    }

    private void OnFleetStateChanged() {
        FleetState fleetState = Model.CurrentState;
        switch (fleetState) {
            case FleetState.ShowDying:
                View.ShowDying();
                break;
            case FleetState.Attacking:
            case FleetState.Dead:
            case FleetState.Disbanding:
            case FleetState.Dying:
            case FleetState.Entrenching:
            case FleetState.ExecuteAttackOrder:
            case FleetState.GoDisband:
            case FleetState.GoGuard:
            case FleetState.GoJoin:
            case FleetState.GoPatrol:
            case FleetState.GoRefit:
            case FleetState.GoRepair:
            case FleetState.GoRetreat:
            case FleetState.Guarding:
            case FleetState.Idling:
            case FleetState.Moving:
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

    private void OnFleetElementDestroyed(ShipModel ship) {
        if (ship.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our fleet's ship that was just destroyed was the focus, so change the focus to the fleet
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    public void __SimulateAllShipsAttacked() {
        Model.Ships.ForAll<ShipItem>(s => s.__SimulateAttacked());
    }

    public Reference<float> GetFleetSpeed() {
        return new Reference<float>(() => Model.Data.CurrentSpeed);
    }

    public void OnPressWhileSelected(bool isDown) {
        OnPressRequestContextMenu(isDown);
    }

    private void OnPressRequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Model.Data.Owner.IsHuman) {
            _cameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(MortalItemDeathEvent e) {
        if ((e.Source as FleetCmdModel) == Model) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public void __OnLeftDoubleClick() {
        Model.ChangeHeading(UnityEngine.Random.insideUnitSphere.normalized);
        Model.ChangeSpeed(UnityEngine.Random.Range(Constants.ZeroF, 2.5F));
    }

    private void OnFleetCompositionChanged() {
        AssessFleetIcon();
    }

    public void OnIntelLevelChanged() {
        Model.Ships.ForAll<ShipItem>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<ShipView>().PlayerIntelLevel = View.PlayerIntelLevel);
        AssessFleetIcon();
    }

    private void OnFlagshipChanged() {
        View.TrackingTarget = GetHQElementTransform();
    }

    public void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
        Model.Ships.ForAll<ShipItem>(s => s.gameObject.GetSafeMonoBehaviourComponent<ShipView>().AssessHighlighting());
    }

    public Transform GetFlagship() {
        return Model.Flagship.transform;
    }

    private AIconFactory _iconFactory = AIconFactory.Instance;
    private void AssessFleetIcon() {
        IIcon fleetIcon;
        GameColor color = GameColor.White;
        // TODO evaluate Composition
        switch (View.PlayerIntelLevel) {
            case IntelLevel.Nil:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.None);
                //color = GameColor.Clear;    // None should be a completely transparent icon
                break;
            case IntelLevel.Unknown:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Unknown);
                // color = GameColor.White;    // may be clear from prior setting
                break;
            case IntelLevel.OutOfDate:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Unknown);
                color = Model.Data.Owner.Color;
                break;
            case IntelLevel.LongRangeSensors:
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Level5);
                color = Model.Data.Owner.Color;
                break;
            case IntelLevel.ShortRangeSensors:
            case IntelLevel.Complete:
                var selectionCriteria = new IconSelectionCriteria[] { IconSelectionCriteria.Level5, IconSelectionCriteria.Science, IconSelectionCriteria.Colony, IconSelectionCriteria.Troop };
                fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, selectionCriteria);
                color = Model.Data.Owner.Color;
                break;
            case IntelLevel.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(View.PlayerIntelLevel));
        }
        D.Log("IntelLevel is {2}, changing {0} to {1}.", typeof(FleetIconIdentity).Name, fleetIcon.Filename, View.PlayerIntelLevel.GetName());
        View.ChangeCmdIcon(fleetIcon, color);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

