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
/// Context Menu Control for <see cref="AUnitBaseCmdItem" />s owned by the User.
/// </summary>
public class BaseCtxControl_User : ACtxControl_User<BaseDirective> {

    private static BaseDirective[] _userMenuOperatorDirectives = new BaseDirective[] {  BaseDirective.Refit,
                                                                                        BaseDirective.Attack,
                                                                                        BaseDirective.Disband,
                                                                                        BaseDirective.Scuttle};

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Disband,
                                                                                        FleetDirective.Refit,
                                                                                        FleetDirective.Repair,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Patrol,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.CloseOrbit };

    private static ShipDirective[] _userRemoteShipDirectives = new ShipDirective[] { ShipDirective.Disband };

    protected override IEnumerable<BaseDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override IEnumerable<ShipDirective> UserRemoteShipDirectives {
        get { return _userRemoteShipDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _baseMenuOperator; } }

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
            case BaseDirective.Attack:
            case BaseDirective.Refit:
            case BaseDirective.Disband:
            case BaseDirective.Scuttle:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(BaseDirective directive, out IEnumerable<INavigableTarget> targets) {
        switch (directive) {
            case BaseDirective.Attack:
                targets = _userKnowledge.Fleets.Where(f => f.Owner.IsAtWarWith(_user)).Cast<INavigableTarget>();    // TODO InRange?
                return true;
            case BaseDirective.Refit:
            case BaseDirective.Disband:
            case BaseDirective.Scuttle:
                targets = Enumerable.Empty<INavigableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Repair:
                var fleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
                return fleet.Data.UnitHealth == Constants.OneHundredPercent && fleet.Data.Health == Constants.OneHundredPercent;
            case FleetDirective.Disband:
            case FleetDirective.Refit:
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
            case FleetDirective.Patrol:
                return !(_baseMenuOperator as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !(_baseMenuOperator as IGuardable).IsGuardingAllowedBy(_user);
            case FleetDirective.CloseOrbit:
                return !(_baseMenuOperator as IShipCloseOrbitable).IsCloseOrbitAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {
        switch (directive) {
            case ShipDirective.Disband:
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
        INavigableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string tgtMsg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _baseMenuOperator.FullName, directive.GetValueName(), tgtMsg);
        _baseMenuOperator.CurrentOrder = new BaseOrder(directive, OrderSource.User, target as IUnitAttackableTarget);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _baseMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        INavigableTarget target = _baseMenuOperator;
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

