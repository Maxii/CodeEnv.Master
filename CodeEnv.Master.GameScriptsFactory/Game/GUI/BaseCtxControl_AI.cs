﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseCtxControl_AI.cs
// Context Menu Control for <see cref="AUnitBaseCommandItem"/>s operated by the AI.
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
/// Context Menu Control for <see cref="AUnitBaseCommandItem"/>s operated by the AI.
/// </summary>
public class BaseCtxControl_AI : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Attack, 
                                                                                                FleetDirective.Move, 
                                                                                                FleetDirective.Guard };
    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private AUnitBaseCommandItem _baseMenuOperator;

    public BaseCtxControl_AI(AUnitBaseCommandItem baseCmd)
        : base(baseCmd.gameObject) {
        _baseMenuOperator = baseCmd;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_baseMenuOperator.Owner);
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_baseMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        IDestinationTarget target = _baseMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
