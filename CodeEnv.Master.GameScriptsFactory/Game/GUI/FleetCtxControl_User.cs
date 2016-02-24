// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCtxControl_User.cs
// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the User.
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
/// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the User.
/// </summary>
public class FleetCtxControl_User : ACtxControl_User<FleetDirective> {

    private static IDictionary<FleetDirective, Speed> _userFleetSpeedLookup = new Dictionary<FleetDirective, Speed>() {
        {FleetDirective.Join, Speed.None },
        {FleetDirective.AssumeFormation, Speed.None },
        {FleetDirective.Repair, Speed.FleetStandard },
        {FleetDirective.Disband, Speed.FleetStandard },
        {FleetDirective.Refit, Speed.FleetStandard },
        {FleetDirective.Scuttle, Speed.None },
        {FleetDirective.Move, Speed.FleetStandard },
        {FleetDirective.Guard, Speed.FleetStandard }
    };

    // OPTIMIZE
    private static IDictionary<ShipDirective, Speed> _userShipSpeedLookup = new Dictionary<ShipDirective, Speed>() {
        {ShipDirective.Join, Speed.None },
    };

    private static FleetDirective[] _userMenuOperatorDirectives = new FleetDirective[] {    FleetDirective.Join,
                                                                                            FleetDirective.AssumeFormation,
                                                                                            FleetDirective.Repair,
                                                                                            FleetDirective.Disband,
                                                                                            FleetDirective.Refit,
                                                                                            FleetDirective.Scuttle };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {     FleetDirective.Join,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.Guard };

    private static ShipDirective[] _userRemoteShipDirectives = new ShipDirective[] { ShipDirective.Join };

    protected override IEnumerable<FleetDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override IEnumerable<ShipDirective> UserRemoteShipDirectives {
        get { return _userRemoteShipDirectives; }
    }

    protected override string OperatorName { get { return _fleetMenuOperator.FullName; } }

    protected override ADiscernibleItem ItemForFindClosest { get { return _fleetMenuOperator; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_User(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject, uniqueSubmenusReqd: 4, menuPosition: MenuPositionMode.Over) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_fleetMenuOperator.IsSelected) {
            D.Assert(_fleetMenuOperator == selected as FleetCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool TryIsSelectedItemUserRemoteShip(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = selected as ShipItem;
        return selectedShip != null && selectedShip.Owner.IsUser;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Repair:
                return _fleetMenuOperator.Data.Health == Constants.OneHundredPercent && _fleetMenuOperator.Data.UnitHealth == Constants.OneHundredPercent;
            case FleetDirective.Join:
            case FleetDirective.AssumeFormation:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
            case FleetDirective.Scuttle:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the item associated with this directive can have a submenu and targets, 
    /// <c>false</c> otherwise. Returns the targets for the subMenu if any were found. Default implementation is false and none.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets.</param>
    /// <returns></returns>
    protected override bool TryGetSubMenuUnitTargets_MenuOperatorIsSelected(FleetDirective directive, out IEnumerable<IUnitAttackableTarget> targets) {
        switch (directive) {
            case FleetDirective.Join:
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(b => b.Owner.IsUser).Except(_fleetMenuOperator).Cast<IUnitAttackableTarget>();
                return true;
            case FleetDirective.Repair:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
                targets = GameObject.FindObjectsOfType<AUnitBaseCmdItem>().Where(b => b.Owner.IsUser).Cast<IUnitAttackableTarget>();
                return true;
            case FleetDirective.Scuttle:
            case FleetDirective.AssumeFormation:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {  // not really needed
        switch (directive) {
            case FleetDirective.Join:
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {   // not really needed
        switch (directive) {
            case ShipDirective.Join:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _fleetMenuOperator.OptimalCameraViewingDistance = _fleetMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_UserMenuOperatorIsSelected(itemID);
        IssueFleetMenuOperatorOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteShipIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteShipIsSelected(itemID);
        IssueRemoteShipOrder(itemID);
    }

    private void IssueFleetMenuOperatorOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        Speed speed = _userFleetSpeedLookup[directive];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _fleetMenuOperator.FullName, directive.GetValueName(), msg);
        _fleetMenuOperator.CurrentOrder = new FleetOrder(directive, OrderSource.User, target, speed);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        Speed speed = _userFleetSpeedLookup[directive];
        INavigableTarget target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target, speed);
    }

    private void IssueRemoteShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        Speed speed = _userShipSpeedLookup[directive];
        INavigableTarget target = _fleetMenuOperator;
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target, speed);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

