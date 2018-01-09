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

    private static FleetDirective[] _userMenuOperatorDirectives = new FleetDirective[]  {
                                                                                            FleetDirective.JoinFleet,
                                                                                            FleetDirective.JoinHanger,
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

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.JoinFleet,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove
                                                                                        };

    protected override IEnumerable<FleetDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _fleetMenuOperator.Position; } }

    protected override string OperatorName { get { return _fleetMenuOperator != null ? _fleetMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _fleetMenuOperator.IsFocus; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_User(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject, uniqueSubmenusReqd: 10, menuPosition: MenuPositionMode.Over) {
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

    /// <summary>
    /// Returns the initial disabled state of the MenuOperator menu item associated with this directive prior to attempting to
    /// populate a submenu for the same menu item. Default implementation returns false, aka not disabled.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool IsUserMenuOperatorMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Disband:
            case FleetDirective.Refit:
            // Insufficient hanger capacity handled by fleet when trying to transfer to hanger
            case FleetDirective.Repair:
            case FleetDirective.Attack:
            case FleetDirective.Scuttle:
            case FleetDirective.ChangeHQ:
            case FleetDirective.JoinFleet:
            case FleetDirective.JoinHanger:
            case FleetDirective.Patrol:
            case FleetDirective.Guard:
            case FleetDirective.Explore:
            case FleetDirective.Withdraw:
            // TODO should be in battle
            case FleetDirective.Retreat:
                // TODO should be in battle
                // IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be the same
                return !_fleetMenuOperator.IsAuthorizedForNewOrder(directive);
            case FleetDirective.AssumeFormation:
                // 12.10.17 IsAuthorizedForNewOrder without provided target is correct as no AssumeFormation targets will be offered
                // IMPROVE Shouldn't order be issuable even if already assuming formation? Previous order could have tgt we no longer want
                return !_fleetMenuOperator.IsAuthorizedForNewOrder(directive) || _fleetMenuOperator.IsCurrentOrderDirectiveAnyOf(directive);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// <remarks>The return value answers the question "Does the directive support submenus?" It does not mean "Are there any targets
    /// in the submenu?" so don't return targets.Any()!</remarks>
    /// <remarks>12.13.17 Avoid asserting anything here based off of the assumption that IsUserMenuOperatorMenuItemDisabledFor will 
    /// have already disabled the selection if the assert would fail. This method is also used to count the number of reqd submenus.</remarks>
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(FleetDirective directive, out IEnumerable<INavigableDestination> targets) {
        bool doesDirectiveSupportSubmenus = false;
        switch (directive) {
            case FleetDirective.JoinFleet:
                IEnumerable<IFleetCmd> joinableFleets;
                _userKnowledge.TryGetJoinableFleetsFor(_fleetMenuOperator, out joinableFleets);
                targets = joinableFleets.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.JoinHanger:
                IEnumerable<IUnitBaseCmd> joinableHangerBases;
                _userKnowledge.TryGetJoinableHangerBasesFor(_fleetMenuOperator, out joinableHangerBases);
                targets = joinableHangerBases.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Patrol:
                // TODO: More selective of patrol friendly systems. Other patrol targets should be explicitly chosen by user
                targets = _userKnowledge.SystemsPatrollableByOwner.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Guard:
                // TODO: More selective of guard friendly systems. Other guard targets should be explicitly chosen by user
                targets = _userKnowledge.SystemsGuardableByOwner.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Explore:
                targets = _userKnowledge.KnownItemsUnexploredByOwnerFleets.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Attack:
                targets = _userKnowledge.KnownItemsAttackableByOwnerUnits.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Repair:
                targets = _userKnowledge.OwnerBases.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Refit:
            case FleetDirective.Disband:
                // Insufficient hanger capacity handled by fleet when trying to transfer to hanger
                targets = _userKnowledge.OwnerBases.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.ChangeHQ:
                targets = _fleetMenuOperator.NonHQElements.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case FleetDirective.Withdraw:   // TODO away from enemy
            case FleetDirective.Retreat:    // TODO away from enemy
            case FleetDirective.Scuttle:
            case FleetDirective.AssumeFormation:    // Note: In-place only, not going to offer LocalAssyStations as targets
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
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this fleet
        switch (directive) {
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return !isOrderAuthorizedByUserRemoteFleet;
            case FleetDirective.JoinFleet:
                return !isOrderAuthorizedByUserRemoteFleet || !_fleetMenuOperator.IsJoinableBy(userRemoteFleet.ElementCount);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _fleetMenuOperator.OptimalCameraViewingDistance = _fleetMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        IssueUserFleetMenuOperatorOrder(itemID);
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueUserFleetMenuOperatorOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableDestination subMenuTarget;
        bool isSubmenuTarget = _unitSubmenuTgtLookup.TryGetValue(itemID, out subMenuTarget);
        string submenuTgtMsg = isSubmenuTarget ? subMenuTarget.DebugName : "[none]";
        D.LogBold("{0} selected directive {1} and submenu target {2} from context menu.", DebugName, directive.GetValueName(), submenuTgtMsg);
        var order = new FleetOrder(directive, OrderSource.User, subMenuTarget);
        _fleetMenuOperator.CurrentOrder = order;
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _fleetMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    #region Archive

    // 11.10.17 Removed menu ability to right click a single ship in another fleet and tell it to join this one. Not interesting.

    ////private static ShipDirective[] _userRemoteShipDirectives = new ShipDirective[] { ShipDirective.Join };

    ////protected override IEnumerable<ShipDirective> UserRemoteShipDirectives {
    ////    get { return _userRemoteShipDirectives; }
    ////}

    ////protected override bool IsUserRemoteShipMenuItemDisabledFor(ShipDirective directive) {
    ////    switch (directive) {
    ////        case ShipDirective.Join:
    ////            return _fleetMenuOperator.IsLoneCmd || !_fleetMenuOperator.IsJoinable;
    ////        default:
    ////            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
    ////    }
    ////}

    ////protected override void HandleMenuPick_UserRemoteShipIsSelected(int itemID) {
    ////    base.HandleMenuPick_UserRemoteShipIsSelected(itemID);
    ////    IssueRemoteUserShipOrder(itemID);
    ////}

    ////private void IssueRemoteUserShipOrder(int itemID) {
    ////    var directive = (ShipDirective)_directiveLookup[itemID];
    ////    D.AssertEqual(ShipDirective.Join, directive);  // HACK
    ////    var remoteShip = _remoteUserOwnedSelectedItem as ShipItem;
    ////    bool isOrderInitiated = remoteShip.InitiateNewOrder(new ShipOrder(directive, OrderSource.User, target: _fleetMenuOperator));
    ////    D.Assert(isOrderInitiated);
    ////}

    #endregion

}

