﻿// --------------------------------------------------------------------------------------------------------------------
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
/// Context Menu Control for <see cref="StarItem"/>s. 
/// No distinction between AI and User owned.
/// </summary>
public class StarCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Move, 
                                                                                                FleetDirective.Patrol, 
                                                                                                FleetDirective.Guard, 
                                                                                                FleetDirective.Explore };

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private StarItem _starMenuOperator;

    public StarCtxControl(StarItem star)
        : base(star.gameObject) {
        _starMenuOperator = star;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Explore:
                // IMPROVE exploring a star is always available? needed to initiate explore of unknown system?
                return false;
            case FleetDirective.Patrol:
                return false;
            case FleetDirective.Move:
                return false;
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_starMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _starMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

