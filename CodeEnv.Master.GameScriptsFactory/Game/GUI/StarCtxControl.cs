// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarCtxControl.cs
// Context Menu Control for <see cref="StarItem"/>s. 
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
/// Context Menu Control for <see cref="StarItem"/>s. 
/// No distinction between AI and User owned.
/// </summary>
public class StarCtxControl : ACtxControl {

    // Note: Stars are not IFleetExplorable, IPatrollable or IGuardable but the Star's System is. The Directives Patrol, Guard 
    // and Explore here for a remote user-owned fleet are simply a convenience for the user to execute a Directive on the System.

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove,
                                                                                            FleetDirective.Patrol,
                                                                                            FleetDirective.Guard,
                                                                                            FleetDirective.Explore
                                                                                        };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _starMenuOperator.Position; } }

    protected override string OperatorName { get { return _starMenuOperator != null ? _starMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _starMenuOperator.IsFocus; } }

    // 6.19.18 since no orders to issue when selected, uses base.SelectedItemMenuHasContent returning false to avoid showing the menu

    private StarItem _starMenuOperator;

    public StarCtxControl(StarItem star)
        : base(star.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _starMenuOperator = star;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_starMenuOperator.IsSelected) {
            D.AssertEqual(_starMenuOperator, selected as StarItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.IsUserOwned;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        FleetCmdItem userRemoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        bool isOrderAuthorizedByUserRemoteFleet = userRemoteFleet.IsAuthorizedForNewOrder(directive);
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this Star
        switch (directive) {
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return !isOrderAuthorizedByUserRemoteFleet;
            case FleetDirective.Explore:
                // A fleet may explore a star(system) if not at war and not already explored
                var explorableSystem = _starMenuOperator.ParentSystem as IFleetExplorable;
                return !isOrderAuthorizedByUserRemoteFleet || !explorableSystem.IsExploringAllowedBy(_user) || explorableSystem.IsFullyExploredBy(_user);
            case FleetDirective.Patrol:
                return !isOrderAuthorizedByUserRemoteFleet || !(_starMenuOperator.ParentSystem as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !isOrderAuthorizedByUserRemoteFleet || !(_starMenuOperator.ParentSystem as IGuardable).IsGuardingAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _starMenuOperator.OptimalCameraViewingDistance = _starMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _starMenuOperator;
        if (directive == FleetDirective.Explore || directive == FleetDirective.Guard || directive == FleetDirective.Patrol) {
            target = _starMenuOperator.ParentSystem as IFleetNavigableDestination;
        }
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

}

