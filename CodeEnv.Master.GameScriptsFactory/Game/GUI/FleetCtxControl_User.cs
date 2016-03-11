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
                                                                                            FleetDirective.Scuttle };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {     FleetDirective.Join,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove,
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

    protected override AItem ItemForDistanceMeasurements { get { return _fleetMenuOperator; } }

    protected override string OperatorName { get { return _fleetMenuOperator.FullName; } }

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
            case FleetDirective.Patrol:
            case FleetDirective.Guard:
            case FleetDirective.Explore:
            case FleetDirective.Attack:
            case FleetDirective.Refit:
            case FleetDirective.Withdraw:   // TODO should be in battle
            case FleetDirective.Retreat:    // TODO should be in battle
            case FleetDirective.Disband:
            case FleetDirective.Scuttle:
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
    protected override bool TryGetSubMenuUnitTargets_MenuOperatorIsSelected(FleetDirective directive, out IEnumerable<INavigableTarget> targets) {
        switch (directive) {
            case FleetDirective.Join:
                targets = _userKnowledge.MyFleets.Except(_fleetMenuOperator).Cast<INavigableTarget>();
                return true;
            case FleetDirective.Patrol:
                // Note: Easy access to patrol friendly systems. Other patrol targets should be explicitly chosen by user
                targets = _userKnowledge.Systems.Where(sys => _fleetMenuOperator.Owner.IsFriendlyWith(sys.Owner)).Cast<INavigableTarget>();
                return true;
            case FleetDirective.Guard:
                // Note: Easy access to guard my systems and starbases. Other guard targets should be explicitly chosen by user
                targets = _userKnowledge.MySystems.Cast<INavigableTarget>().Union(_userKnowledge.MyStarbases.Cast<INavigableTarget>());
                return true;
            case FleetDirective.Explore:
                // Note: Easy access to explore systems allowing exploration that need it. Other exploration targets should be explicitly chosen by user
                var systems = _userKnowledge.Systems.Cast<IFleetExplorable>();
                var systemsAllowingExploration = systems.Where(sys => sys.IsExplorationAllowedBy(_fleetMenuOperator.Owner));
                var systemsNeedingExploration = systemsAllowingExploration.Where(sys => !sys.IsFullyExploredBy(_fleetMenuOperator.Owner)).Cast<INavigableTarget>();
                targets = systemsNeedingExploration;
                return true;
            case FleetDirective.Attack:
                // Note: Easy access to attack fleets and bases of war opponents. Other attack targets should be explicitly chosen by user
                var knownFleetsAtWar = _userKnowledge.Fleets.Where(f => _fleetMenuOperator.Owner.IsAtWarWith(f.Owner));
                var knownBasesAtWar = _userKnowledge.Bases.Where(b => _fleetMenuOperator.Owner.IsAtWarWith(b.Owner));
                targets = knownFleetsAtWar.Cast<INavigableTarget>().Union(knownBasesAtWar.Cast<INavigableTarget>());
                return true;
            case FleetDirective.Repair:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
            case FleetDirective.Withdraw:   // TODO away from enemy
            case FleetDirective.Retreat:    // TODO away from enemy
                targets = _userKnowledge.MyBases.Cast<INavigableTarget>();
                return true;
            case FleetDirective.Scuttle:
            case FleetDirective.AssumeFormation:
                targets = Enumerable.Empty<INavigableTarget>();
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
        INavigableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _fleetMenuOperator.FullName, directive.GetValueName(), msg);
        _fleetMenuOperator.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    private void IssueRemoteShipOrder(int itemID) {
        var directive = (ShipDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

