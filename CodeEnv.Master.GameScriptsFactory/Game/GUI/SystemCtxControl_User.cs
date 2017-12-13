// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl_User.cs
// Context Menu Control for <see cref="SystemItem"/>s owned by the User.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="SystemItem"/>s owned by the User.
/// </summary>
public class SystemCtxControl_User : ACtxControl_User<BaseDirective> {

    private static BaseDirective[] _userMenuOperatorDirectives = new BaseDirective[] {
                                                                                        BaseDirective.Repair,
                                                                                        BaseDirective.Disband,
                                                                                        BaseDirective.Scuttle,
                                                                                        BaseDirective.Refit,
                                                                                        BaseDirective.Attack
                                                                                     };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {
                                                                                        FleetDirective.Disband,
                                                                                        FleetDirective.Refit,
                                                                                        FleetDirective.Repair,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.Patrol
                                                                                      };

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

    protected override Vector3 PositionForDistanceMeasurements { get { return _settlement.Position; } }

    protected override string OperatorName { get { return _systemMenuOperator != null ? _systemMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _systemMenuOperator.IsFocus; } }

    private SystemItem _systemMenuOperator;
    private SettlementCmdItem _settlement;

    public SystemCtxControl_User(SystemItem system)
        : base(system.gameObject, uniqueSubmenusReqd: 1, menuPosition: MenuPositionMode.AtCursor) {
        _systemMenuOperator = system;
        _settlement = system.Settlement;
        D.AssertNotNull(_settlement);
        __ValidateUniqueSubmenuQtyReqd();
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_systemMenuOperator.IsSelected) {
            D.AssertEqual(_systemMenuOperator, selected as SystemItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.IsUserOwned;
    }

    protected override bool TryIsSelectedItemUserRemoteShip(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = selected as ShipItem;
        return selectedShip != null && selectedShip.IsUserOwned;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Disband:
            case BaseDirective.Refit:
            case BaseDirective.Repair:
            // IsCurrentOrderDirectiveAnyOf not needed as IsAuthorizedForNewOrder will correctly respond once order processed
            ////return !_settlement.IsAuthorizedForNewOrder(directive) || _settlement.IsCurrentOrderDirectiveAnyOf(directive);
            case BaseDirective.Scuttle:
            case BaseDirective.Attack:
                return !_settlement.IsAuthorizedForNewOrder(directive);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// <remarks>The return value answers the question "Does the directive support submenus?" It does not mean "Are there any targets
    /// in the submenu?" so don't return targets.Any()!</remarks>
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(BaseDirective directive, out IEnumerable<INavigableDestination> targets) {
        bool doesDirectiveSupportSubmenus = false;
        switch (directive) {
            case BaseDirective.Attack:
                targets = _userKnowledge.KnownFleetsAttackableByOwnerUnits.Cast<INavigableDestination>();   // TODO add InRange
                doesDirectiveSupportSubmenus = true;
                break;
            case BaseDirective.Repair:
            case BaseDirective.Refit:
            case BaseDirective.Disband:
            case BaseDirective.Scuttle:
                targets = Enumerable.Empty<INavigableDestination>();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
        return doesDirectiveSupportSubmenus;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        FleetCmdItem userRemoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        bool isOrderAuthorizedByUserRemoteFleet = userRemoteFleet.IsAuthorizedForNewOrder(directive);
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this Base
        switch (directive) {
            case FleetDirective.Refit:
            case FleetDirective.Disband:
                // only allows order if this specific Base hanger has room
                return !isOrderAuthorizedByUserRemoteFleet || !_settlement.Hanger.IsJoinableBy(userRemoteFleet.ElementCount);
            case FleetDirective.Repair:
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Patrol:
            case FleetDirective.Guard:
                return !isOrderAuthorizedByUserRemoteFleet;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {
        ShipItem userRemoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        // userRemoteShip.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this Base
        switch (directive) {
            case ShipDirective.Disband:
                // 12.5.17 Currently no CtxMenu orders allowed for ships in hangers
                return userRemoteShip.IsLocatedInHanger || !userRemoteShip.IsAuthorizedForNewOrder(directive) || !_settlement.Hanger.IsJoinable;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _systemMenuOperator.OptimalCameraViewingDistance = _systemMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        IssueUserSystemMenuOperatorOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteShipIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteShipIsSelected(itemID);
        IssueRemoteUserShipOrder(itemID);
    }

    private void IssueUserSystemMenuOperatorOrder(int itemID) {
        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        INavigableDestination target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.DebugName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", DebugName, directive.GetValueName(), msg);
        var order = new BaseOrder(directive, OrderSource.User, target as IUnitAttackable);
        _settlement.CurrentOrder = order;
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        var directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _systemMenuOperator;
        if (directive == FleetDirective.Disband || directive == FleetDirective.Refit || directive == FleetDirective.Repair) {
            target = _settlement;
        }
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    private void IssueRemoteUserShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        D.AssertEqual(ShipDirective.Disband, directive);   // HACK
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target: _settlement);
    }

}

