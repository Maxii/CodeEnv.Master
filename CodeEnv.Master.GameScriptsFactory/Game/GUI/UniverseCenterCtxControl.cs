// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterCtxControl.cs
// Context Menu Control for <see cref="UniverseCenterItem"/>.
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
/// Context Menu Control for <see cref="UniverseCenterItem"/>.
/// No distinction between AI and User owned.    
/// <remarks>OPTIMIZE 8.7.17 these non-user-owned Items aren't selectable.</remarks>
/// </summary>
public class UniverseCenterCtxControl : ACtxControl {

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

    protected override Vector3 PositionForDistanceMeasurements { get { return _universeCenterMenuOperator.Position; } }

    protected override string OperatorName { get { return _universeCenterMenuOperator != null ? _universeCenterMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _universeCenterMenuOperator.IsFocus; } }

    private UniverseCenterItem _universeCenterMenuOperator;

    public UniverseCenterCtxControl(UniverseCenterItem universeCenter)
        : base(universeCenter.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _universeCenterMenuOperator = universeCenter;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_universeCenterMenuOperator.IsSelected) {
            D.AssertEqual(_universeCenterMenuOperator, selected as UniverseCenterItem);
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
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be UCenter
        switch (directive) {
            // Note: UCenter has no owner and therefore by definition exploring, guarding and patrolling is allowed
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Patrol:
            case FleetDirective.Guard:
                return !isOrderAuthorizedByUserRemoteFleet;
            case FleetDirective.Explore:
                var explorableUCenter = _universeCenterMenuOperator as IFleetExplorable;
                return !isOrderAuthorizedByUserRemoteFleet || explorableUCenter.IsFullyExploredBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _universeCenterMenuOperator.OptimalCameraViewingDistance = _universeCenterMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _universeCenterMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

}

