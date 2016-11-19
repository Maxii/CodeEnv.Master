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

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Attack,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.Explore,
                                                                                        FleetDirective.Patrol
                                                                                      };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _settlement.Position; } }

    protected override string OperatorName { get { return _systemMenuOperator.FullName; } }

    private SystemItem _systemMenuOperator;
    private SettlementCmdItem _settlement;

    public SystemCtxControl_AI(SystemItem system)
        : base(system.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.AtCursor) {
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
        switch (directive) {
            case FleetDirective.Attack:
                return !(_settlement as IUnitAttackable).IsAttackByAllowed(_user)
                    || !(_remoteUserOwnedSelectedItem as AUnitCmdItem).IsAttackCapable;
            case FleetDirective.Explore:
                var explorableSystem = _systemMenuOperator as IFleetExplorable;
                return !explorableSystem.IsExploringAllowedBy(_user) || explorableSystem.IsFullyExploredBy(_user);
            case FleetDirective.Patrol:
                return !(_systemMenuOperator as IPatrollable).IsPatrollingAllowedBy(_user);
            case FleetDirective.Guard:
                return !(_systemMenuOperator as IGuardable).IsGuardingAllowedBy(_user);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
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
        IFleetNavigable target = directive == FleetDirective.Attack ? _settlement as IFleetNavigable : _systemMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

