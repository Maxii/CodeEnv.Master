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

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Patrol,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.Explore };
    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _starMenuOperator.Position; } }

    protected override string OperatorName { get { return _starMenuOperator.DebugName; } }

    private StarItem _starMenuOperator;

    public StarCtxControl(StarItem star)
        : base(star.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
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
        switch (directive) {
            case FleetDirective.Explore:
                // A fleet may explore a star(system) if not at war and not already explored
                var explorableSystem = _starMenuOperator.ParentSystem as IFleetExplorable;
                return explorableSystem.IsFullyExploredBy(_user) || !explorableSystem.IsExploringAllowedBy(_user);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                // A fleet may move to any star without regard to Diplo state
                return false;
            case FleetDirective.Patrol:
                return !(_starMenuOperator.ParentSystem as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !(_starMenuOperator.ParentSystem as IGuardable).IsGuardingAllowedBy(_user);
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
        IFleetNavigable target = _starMenuOperator;
        if (directive == FleetDirective.Explore || directive == FleetDirective.Guard || directive == FleetDirective.Patrol) {
            target = _starMenuOperator.ParentSystem as IFleetNavigable;
        }
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

