// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCtxControl_AI.cs
// Context Menu Control for <see cref="FleetCommandItem"/>s operated by the AI.
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
/// Context Menu Control for <see cref="FleetCommandItem"/>s operated by the AI.
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

    private FleetCommandItem _fleetMenuOperator;

    public FleetCtxControl_AI(FleetCommandItem fleetCmd)
        : base(fleetCmd.gameObject) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
    }

    protected override bool TryIsRemoteBaseAccessAttempted(ISelectable selected, out AUnitBaseCommandItem selectedBase) {
        selectedBase = selected as AUnitBaseCommandItem;
        return selectedBase != null && selectedBase.Owner.IsPlayer;
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
        IDestinationTarget target = _fleetMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    protected override void OnMenuSelection_RemoteBaseAccess(int itemID) {
        base.OnMenuSelection_RemoteBaseAccess(itemID);

        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitTarget target = _fleetMenuOperator;
        var remoteBase = _remotePlayerOwnedSelectedItem as AUnitBaseCommandItem;
        remoteBase.CurrentOrder = new BaseOrder(directive, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

