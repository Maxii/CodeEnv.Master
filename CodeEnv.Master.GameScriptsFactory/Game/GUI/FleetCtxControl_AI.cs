// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCtxControl_AI.cs
// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the AI.
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

/// <summary>
/// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the AI.
/// </summary>
public class FleetCtxControl_AI : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {    FleetDirective.Attack,
                                                                                           FleetDirective.Move,
                                                                                           FleetDirective.FullSpeedMove};

    private static BaseDirective[] _userRemoteBaseDirectives = new BaseDirective[] { BaseDirective.Attack };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override IEnumerable<BaseDirective> UserRemoteBaseDirectives {
        get { return _userRemoteBaseDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _fleetMenuOperator; } }

    protected override string OperatorName { get { return _fleetMenuOperator.FullName; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_AI(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Over) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_fleetMenuOperator.IsSelected) {
            D.Assert(_fleetMenuOperator == selected as FleetCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteShip(ISelectable selected, out ShipItem selectedShip) {
        return base.TryIsSelectedItemUserRemoteShip(selected, out selectedShip);
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool TryIsSelectedItemUserRemoteBase(ISelectable selected, out AUnitBaseCmdItem selectedBase) {
        selectedBase = selected as AUnitBaseCmdItem;
        return selectedBase != null && selectedBase.Owner.IsUser;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_user.IsEnemyOf(_fleetMenuOperator.Owner);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteBaseMenuItemDisabledFor(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Attack:
                return !_user.IsEnemyOf(_fleetMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _fleetMenuOperator.OptimalCameraViewingDistance = _fleetMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteBaseIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteBaseIsSelected(itemID);
        IssueRemoteBaseOrder(itemID);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteBaseOrder(int itemID) {
        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target = _fleetMenuOperator;
        var remoteBase = _remoteUserOwnedSelectedItem as AUnitBaseCmdItem;
        remoteBase.CurrentOrder = new BaseOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

