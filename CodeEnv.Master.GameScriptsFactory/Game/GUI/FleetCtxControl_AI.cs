// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCtxControl_AI.cs
// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the AI.
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
/// Context Menu Control for <see cref="FleetCmdItem"/>s owned by the AI.
/// </summary>
public class FleetCtxControl_AI : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Attack, 
                                                                                                FleetDirective.Move, 
                                                                                                FleetDirective.Guard };

    private static BaseDirective[] _remoteBaseDirectivesAvailable = new BaseDirective[] { BaseDirective.Attack };

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override IEnumerable<BaseDirective> RemoteBaseDirectives {
        get { return _remoteBaseDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_AI(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        D.Warn("You are trying to open a ContextMenu that doesn't belong to you!");
        return base.TryIsSelectedItemAccessAttempted(selected);
    }

    protected override bool TryIsRemoteShipAccessAttempted(ISelectable selected, out ShipItem selectedShip) {
        return base.TryIsRemoteShipAccessAttempted(selected, out selectedShip);
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool TryIsRemoteBaseAccessAttempted(ISelectable selected, out AUnitBaseCmdItem selectedBase) {
        selectedBase = selected as AUnitBaseCmdItem;
        return selectedBase != null && selectedBase.Owner.IsUser;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_fleetMenuOperator.Owner);
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_fleetMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteBaseMenuItemDisabled(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Attack:
                return !_remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_fleetMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    protected override void OnMenuSelection_RemoteBaseAccess(int itemID) {
        base.OnMenuSelection_RemoteBaseAccess(itemID);

        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target = _fleetMenuOperator;
        var remoteBase = _remotePlayerOwnedSelectedItem as AUnitBaseCmdItem;
        remoteBase.CurrentOrder = new BaseOrder(directive, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

