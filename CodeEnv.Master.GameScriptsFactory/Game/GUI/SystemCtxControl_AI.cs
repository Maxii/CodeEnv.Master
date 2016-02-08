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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Context Menu Control for <see cref="SystemItem"/>s owned by the AI.
/// </summary>
public class SystemCtxControl_AI : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Attack,
                                                                                                FleetDirective.Move,
                                                                                                FleetDirective.Guard,
                                                                                                FleetDirective.Explore };
    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override string OperatorName { get { return _systemMenuOperator.FullName; } }

    private SystemItem _systemMenuOperator;
    private SettlementCmdItem _settlement;

    public SystemCtxControl_AI(SystemItem system)
        : base(system.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.AtCursor) {
        _systemMenuOperator = system;
        _settlement = system.Settlement;
        D.Assert(_settlement != null);
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_systemMenuOperator.IsSelected) {
            D.Assert(_systemMenuOperator == selected as SystemItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_remoteUserOwnedSelectedItem.Owner.IsEnemyOf(_systemMenuOperator.Owner);
            case FleetDirective.Explore:
                return false; //TODO _systemMenuOperator.HumanPlayerIntelCoverage == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
                return false;
            case FleetDirective.Guard:
                return _remoteUserOwnedSelectedItem.Owner.IsEnemyOf(_systemMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuSelection_OptimalFocusDistance() {
        _systemMenuOperator.OptimalCameraViewingDistance = _systemMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuSelection_RemoteFleetAccess(int itemID) {
        base.HandleMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = directive == FleetDirective.Attack ? _settlement as INavigableTarget : _systemMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

