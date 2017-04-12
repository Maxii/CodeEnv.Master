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
/// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the User.
/// </summary>
public class FleetCtxControl_User : ACtxControl_User<FleetDirective> {

    private static FleetDirective[] _userMenuOperatorDirectives = new FleetDirective[] {    FleetDirective.Join,
                                                                                            FleetDirective.AssumeFormation,
                                                                                            FleetDirective.Patrol,
                                                                                            FleetDirective.Guard,
                                                                                            FleetDirective.Explore,
                                                                                            FleetDirective.Attack,
                                                                                            FleetDirective.Repair,
                                                                                            FleetDirective.Disband,
                                                                                            FleetDirective.Refit,
                                                                                            FleetDirective.Withdraw,
                                                                                            FleetDirective.Retreat,
                                                                                            FleetDirective.Scuttle,
                                                                                            FleetDirective.ChangeHQ
                                                                                        };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {     FleetDirective.Join,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove };

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

    protected override Vector3 PositionForDistanceMeasurements { get { return _fleetMenuOperator.Position; } }

    protected override string OperatorName { get { return _fleetMenuOperator.DebugName; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_User(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject, uniqueSubmenusReqd: 11, menuPosition: MenuPositionMode.Over) {
        _fleetMenuOperator = fleetCmd;
        __ValidateUniqueSubmenuQtyReqd();
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_fleetMenuOperator.IsSelected) {
            D.AssertEqual(_fleetMenuOperator, selected as FleetCmdItem);
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

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Repair:
                return _fleetMenuOperator.Data.Health == Constants.OneHundredPercent && _fleetMenuOperator.Data.UnitHealth == Constants.OneHundredPercent;
            case FleetDirective.Attack:
                return !_fleetMenuOperator.IsAttackCapable;
            case FleetDirective.AssumeFormation:
            case FleetDirective.Scuttle:
                return _fleetMenuOperator.IsCurrentOrderDirectiveAnyOf(directive);
            case FleetDirective.ChangeHQ:
                return _fleetMenuOperator.Elements.Count == Constants.One;
            case FleetDirective.Disband:
            case FleetDirective.Join:
            case FleetDirective.Patrol:
            case FleetDirective.Guard:
            case FleetDirective.Explore:
            case FleetDirective.Refit:
            case FleetDirective.Withdraw:   // TODO should be in battle
            case FleetDirective.Retreat:    // TODO should be in battle
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
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(FleetDirective directive, out IEnumerable<INavigable> targets) {
        switch (directive) {
            case FleetDirective.Join:
                targets = _userKnowledge.OwnerFleets.Except(_fleetMenuOperator).Cast<INavigable>();
                return true;
            case FleetDirective.Patrol:
                // TODO: More selective of patrol friendly systems. Other patrol targets should be explicitly chosen by user
                targets = _userKnowledge.Systems.Cast<IPatrollable>().Where(sys => sys.IsPatrollingAllowedBy(_user)).Cast<INavigable>();
                return true;
            case FleetDirective.Guard:
                // Note: Easy access to guard my systems and bases. Other guard targets should be explicitly chosen by user
                targets = _userKnowledge.OwnerSystems.Cast<INavigable>().Union(_userKnowledge.OwnerBases.Cast<INavigable>());
                return true;
            case FleetDirective.Explore:
                // Note: Easy access to explore systems allowing exploration that need it. Other exploration targets should be explicitly chosen by user
                var systems = _userKnowledge.Systems.Cast<IFleetExplorable>();
                var systemsAllowingExploration = systems.Where(sys => sys.IsExploringAllowedBy(_user));
                var systemsNeedingExploration = systemsAllowingExploration.Where(sys => !sys.IsFullyExploredBy(_user)).Cast<INavigable>();
                targets = systemsNeedingExploration;
                return true;
            case FleetDirective.Attack:
                targets = _userKnowledge.Commands.Cast<IUnitAttackable>().Where(cmd => cmd.IsWarAttackByAllowed(_user)).Cast<INavigable>();
                return true;
            case FleetDirective.Repair:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
            case FleetDirective.Withdraw:   // TODO away from enemy
            case FleetDirective.Retreat:    // TODO away from enemy
                targets = _userKnowledge.OwnerBases.Cast<INavigable>();
                return true;
            case FleetDirective.ChangeHQ:
                targets = _fleetMenuOperator.Elements.Except(_fleetMenuOperator.HQElement).Cast<INavigable>();
                return true;
            case FleetDirective.Scuttle:
            case FleetDirective.AssumeFormation:    // Note: In-place only, not going to offer LocalAssyStations as targets
                targets = Enumerable.Empty<INavigable>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {  // not really needed
        switch (directive) {
            case FleetDirective.Join:
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Guard:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {
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
        IssueUserFleetMenuOperatorOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteShipIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteShipIsSelected(itemID);
        IssueRemoteUserShipOrder(itemID);
    }

    private void IssueUserFleetMenuOperatorOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigable target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.DebugName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _fleetMenuOperator.DebugName, directive.GetValueName(), msg);
        if (directive == FleetDirective.ChangeHQ) {
            _fleetMenuOperator.HQElement = target as ShipItem;
        }
        else {
            _fleetMenuOperator.CurrentOrder = new FleetOrder(directive, OrderSource.User, target as IFleetNavigable);
        }
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigable target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteUserShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        D.AssertEqual(ShipDirective.Join, directive);  // HACK
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target: _fleetMenuOperator);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

