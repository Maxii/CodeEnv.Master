﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidCtxControl.cs
// Context Menu Control for <see cref="APlanetoidItem"/>s.
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
/// Context Menu Control for <see cref="APlanetoidItem"/>s. 
/// No distinction between AI and Player owned.
/// </summary>
public class PlanetoidCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Move, 
                                                                                                FleetDirective.Attack, 
                                                                                                FleetDirective.Guard, 
                                                                                                FleetDirective.Explore };

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private APlanetoidItem _planetoidMenuOperator;

    public PlanetoidCtxControl(APlanetoidItem planetoid)
        : base(planetoid.gameObject) {
        _planetoidMenuOperator = planetoid;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_planetoidMenuOperator.Owner);
            case FleetDirective.Explore:
                return _planetoidMenuOperator.PlayerIntelCoverage == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_planetoidMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _planetoidMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

