// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl.cs
// Context Menu Control for <see cref="SystemItem"/>s with no owner (and therefore no settlement).
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
/// Context Menu Control for <see cref="SystemItem"/>s with no owner (and therefore no settlement).
/// <remarks>OPTIMIZE 8.7.17 these non-user-owned Items aren't selectable.</remarks>
/// </summary>
public class SystemCtxControl : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.FullSpeedMove,
                                                                                            FleetDirective.Guard,
                                                                                            FleetDirective.Explore,
                                                                                            FleetDirective.Patrol
                                                                                        };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _systemMenuOperator.Position; } }

    protected override string OperatorName { get { return _systemMenuOperator != null ? _systemMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _systemMenuOperator.IsFocus; } }

    private SystemItem _systemMenuOperator;

    public SystemCtxControl(SystemItem system)
        : base(system.gameObject, uniqueSubmenuQtyReqd: Constants.One, menuPosition: MenuPositionMode.AtCursor) {
        _systemMenuOperator = system;
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

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            // Note: Systems without an owner are by definition explorable, guardable and patrollable
            case FleetDirective.Explore:
                var explorableSystem = _systemMenuOperator as IFleetExplorable;
                return explorableSystem.IsFullyExploredBy(_user) || !explorableSystem.IsExploringAllowedBy(_user);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Patrol:
                return !(_systemMenuOperator as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !(_systemMenuOperator as IGuardable).IsGuardingAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _systemMenuOperator.OptimalCameraViewingDistance = _systemMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteUserFleetOrder(itemID);
    }

    private void IssueRemoteUserFleetOrder(int itemID) {
        var directive = (FleetDirective)_directiveLookup[itemID];
        IFleetNavigableDestination target = _systemMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    #region Targeting OrbitalPlane point Archive

    // Approach used to allow "System" target to be any point on the OrbitalPlane
    //private void IssueFleetOrder(int itemID) {
    //    var directive = (FleetDirective)_directiveLookup[itemID];
    //    INavigableTarget target = new StationaryLocation(_lastPressReleasePosition);
    //    var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
    //    remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    //}

    #endregion

}

