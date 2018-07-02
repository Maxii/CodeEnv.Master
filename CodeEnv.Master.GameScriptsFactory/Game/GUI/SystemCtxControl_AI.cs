// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl_AI.cs
// Context Menu Control for <see cref="SystemItem"/>s owned by the AI.
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
/// Context Menu Control for <see cref="SystemItem"/>s owned by the AI.
/// </summary>
public class SystemCtxControl_AI : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[]   {
                                                                                            FleetDirective.Attack,
                                                                                            FleetDirective.FullSpeedMove,
                                                                                            FleetDirective.Move,
                                                                                            FleetDirective.Guard,
                                                                                            FleetDirective.Explore,
                                                                                            FleetDirective.Patrol,
                                                                                            FleetDirective.FoundSettlement,
                                                                                            FleetDirective.FoundStarbase
                                                                                        };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _settlement.Position; } }

    protected override string OperatorName { get { return _systemMenuOperator != null ? _systemMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _systemMenuOperator.IsFocus; } }

    // 6.19.18 since no orders to issue when selected, uses base.SelectedItemMenuHasContent returning false to avoid showing the menu

    private SystemItem _systemMenuOperator;
    private SettlementCmdItem _settlement;

    public SystemCtxControl_AI(SystemItem system)
        : base(system.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.AtCursor) {
        _systemMenuOperator = system;
        _settlement = system.Settlement;
        D.AssertNotNull(_settlement);
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
        FleetCmdItem userRemoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        bool isOrderAuthorizedByUserRemoteFleet = userRemoteFleet.IsAuthorizedForNewOrder(directive);
        // userRemoteFleet.IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be this AIBase
        switch (directive) {
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return !isOrderAuthorizedByUserRemoteFleet;
            case FleetDirective.Attack:
                return !isOrderAuthorizedByUserRemoteFleet || !(_settlement as IUnitAttackable).IsAttackAllowedBy(_user);
            case FleetDirective.Patrol:
                return !isOrderAuthorizedByUserRemoteFleet || !(_settlement as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !isOrderAuthorizedByUserRemoteFleet || !(_settlement as IGuardable).IsGuardingAllowedBy(_user);
            case FleetDirective.Explore:
                var explorableSystem = _systemMenuOperator as IFleetExplorable;
                return !isOrderAuthorizedByUserRemoteFleet || !explorableSystem.IsExploringAllowedBy(_user) || explorableSystem.IsFullyExploredBy(_user);
            case FleetDirective.FoundSettlement:
                // 6.14.18 Although Systems owned by the AI are already settled by the AI, that doesn't mean the User knows its AI-owned
                return !isOrderAuthorizedByUserRemoteFleet || !_systemMenuOperator.IsFoundingSettlementAllowedBy(_user);
            case FleetDirective.FoundStarbase:
                // 6.14.18 Although Systems owned by the AI means the Sector is too, that doesn't mean the User knows its AI-owned
                // Starbases can't be founded inside Systems, but they can be founded in the System's Sector
                var systemSector = SectorGrid.Instance.GetSector(_systemMenuOperator.SectorID);
                return !isOrderAuthorizedByUserRemoteFleet || !systemSector.IsFoundingStarbaseAllowedBy(_user);
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
        IFleetNavigableDestination target = GetFleetTarget(directive);
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        var order = new FleetOrder(directive, OrderSource.User, target);
        remoteFleet.CurrentOrder = order;
    }

    private IFleetNavigableDestination GetFleetTarget(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return _settlement;
            case FleetDirective.FoundStarbase:
                var systemSector = SectorGrid.Instance.GetSector(_systemMenuOperator.SectorID);
                return systemSector;
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Move:
            case FleetDirective.Guard:
            case FleetDirective.Explore:
            case FleetDirective.Patrol:
            case FleetDirective.FoundSettlement:
                return _systemMenuOperator;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

}

