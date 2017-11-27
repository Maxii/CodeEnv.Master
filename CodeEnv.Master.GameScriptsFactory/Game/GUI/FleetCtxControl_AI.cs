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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the AI.
/// </summary>
public class FleetCtxControl_AI : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {   FleetDirective.Attack,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove
                                                                                        };

    private static BaseDirective[] _userRemoteBaseDirectives = new BaseDirective[] { BaseDirective.Attack };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override IEnumerable<BaseDirective> UserRemoteBaseDirectives {
        get { return _userRemoteBaseDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _fleetMenuOperator.Position; } }

    protected override string OperatorName { get { return _fleetMenuOperator != null ? _fleetMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _fleetMenuOperator.IsFocus; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_AI(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Over) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_fleetMenuOperator.IsSelected) {
            D.AssertEqual(_fleetMenuOperator, selected as FleetCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteShip(ISelectable selected, out ShipItem selectedShip) {
        return base.TryIsSelectedItemUserRemoteShip(selected, out selectedShip);
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.IsUserOwned;
    }

    protected override bool TryIsSelectedItemUserRemoteBase(ISelectable selected, out AUnitBaseCmdItem selectedBase) {
        selectedBase = selected as AUnitBaseCmdItem;
        return selectedBase != null && selectedBase.IsUserOwned;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !(_fleetMenuOperator as IUnitAttackable).IsAttackAllowedBy(_user)
                    || !(_remoteUserOwnedSelectedItem as AUnitCmdItem).IsAttackCapable;
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
                return !(_fleetMenuOperator as IUnitAttackable).IsAttackAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _fleetMenuOperator.OptimalCameraViewingDistance = _fleetMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteBaseIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteBaseIsSelected(itemID);
        IssueRemoteUserBaseOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    private void IssueRemoteUserBaseOrder(int itemID) {
        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitAttackable target = _fleetMenuOperator;
        var remoteBase = _remoteUserOwnedSelectedItem as AUnitBaseCmdItem;
        var order = new BaseOrder(directive, OrderSource.User, target);
        remoteBase.CurrentOrder = order;
    }

}

