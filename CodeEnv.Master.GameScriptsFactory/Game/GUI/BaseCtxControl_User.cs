// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseCtxControl_User.cs
// Context Menu Control for <see cref="AUnitBaseCommandItem"/>s operated by the User.
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
/// Context Menu Control for <see cref="AUnitBaseCmdItem"/>s owned by the User.
/// </summary>
public class BaseCtxControl_User : ACtxControl_User<BaseDirective> {

    private static IDictionary<FleetDirective, Speed> _userFleetSpeedLookup = new Dictionary<FleetDirective, Speed>() {
        {FleetDirective.Repair, Speed.FleetStandard },
        {FleetDirective.Disband, Speed.FleetStandard },
        {FleetDirective.Refit, Speed.FleetStandard },
        {FleetDirective.Move, Speed.FleetStandard },
        {FleetDirective.Guard, Speed.FleetStandard },
    };

    // OPTIMIZE
    private static IDictionary<ShipDirective, Speed> _userShipSpeedLookup = new Dictionary<ShipDirective, Speed>() {
        {ShipDirective.Disband, Speed.None },
        {ShipDirective.Refit, Speed.None },
    };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Disband,
                                                                                        FleetDirective.Refit,
                                                                                        FleetDirective.Repair,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.Guard };

    private static BaseDirective[] _userMenuOperatorDirectives = new BaseDirective[] {  BaseDirective.Repair,
                                                                                        BaseDirective.Refit,
                                                                                        BaseDirective.Attack,
                                                                                        BaseDirective.Disband,
                                                                                        BaseDirective.Scuttle};

    private static ShipDirective[] _userRemoteShipDirectives = new ShipDirective[] {   ShipDirective.Disband,
                                                                                       ShipDirective.Refit };

    protected override IEnumerable<BaseDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override IEnumerable<ShipDirective> UserRemoteShipDirectives {
        get { return _userRemoteShipDirectives; }
    }

    protected override ADiscernibleItem ItemForFindClosest { get { return _baseMenuOperator; } }

    protected override string OperatorName { get { return _baseMenuOperator.FullName; } }

    private AUnitBaseCmdItem _baseMenuOperator;

    public BaseCtxControl_User(AUnitBaseCmdItem baseCmd)
        : base(baseCmd.gameObject, uniqueSubmenusReqd: 1, menuPosition: MenuPositionMode.Over) {
        _baseMenuOperator = baseCmd;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_baseMenuOperator.IsSelected) {
            D.Assert(_baseMenuOperator == selected as AUnitBaseCmdItem);
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

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Repair:
                return _baseMenuOperator.Data.Health == Constants.OneHundredPercent && _baseMenuOperator.Data.UnitHealth == Constants.OneHundredPercent;
            case BaseDirective.Attack:
            //TODO
            case BaseDirective.Refit:
            //TODO
            case BaseDirective.Disband:
            case BaseDirective.Scuttle:
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
    protected override bool TryGetSubMenuUnitTargets_MenuOperatorIsSelected(BaseDirective directive, out IEnumerable<IUnitAttackableTarget> targets) {
        switch (directive) {
            case BaseDirective.Attack:
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(f => f.Owner.IsEnemyOf(_baseMenuOperator.Owner)).Cast<IUnitAttackableTarget>(); ;
                return true;
            case BaseDirective.Disband:
            case BaseDirective.Refit:
            case BaseDirective.Repair:
            case BaseDirective.Scuttle:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {  // not really needed as base returns false
        switch (directive) {
            case FleetDirective.Repair:
                var fleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
                return fleet.Data.UnitHealth == Constants.OneHundredPercent && fleet.Data.Health == Constants.OneHundredPercent;
            case FleetDirective.Disband:
            case FleetDirective.Refit:
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {   // not really needed as base returns false
        switch (directive) {
            case ShipDirective.Disband:
            case ShipDirective.Refit:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _baseMenuOperator.OptimalCameraViewingDistance = _baseMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_UserMenuOperatorIsSelected(itemID);
        IssueBaseMenuOperatorOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteShipIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteShipIsSelected(itemID);
        IssueRemoteShipOrder(itemID);
    }

    private void IssueBaseMenuOperatorOrder(int itemID) {
        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string tgtMsg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _baseMenuOperator.FullName, directive.GetValueName(), tgtMsg);
        _baseMenuOperator.CurrentOrder = new BaseOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        var directive = (FleetDirective)_directiveLookup[itemID];
        Speed speed = _userFleetSpeedLookup[directive];
        INavigableTarget target = _baseMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target, speed);
    }

    private void IssueRemoteShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        Speed speed = _userShipSpeedLookup[directive];
        INavigableTarget target = _baseMenuOperator;
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target, speed);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

