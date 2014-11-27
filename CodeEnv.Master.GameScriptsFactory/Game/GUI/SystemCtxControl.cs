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
/// Context Menu Control for <see cref="SystemItem"/>s with no owner (and therefore no settlement).
/// </summary>
public class SystemCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Move, 
                                                                                                FleetDirective.Guard, 
                                                                                                FleetDirective.Explore,
                                                                                                FleetDirective.Patrol };

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return 1; } }

    private SystemItem _systemMenuOperator;

    public SystemCtxControl(SystemItem system)
        : base(system.gameObject) {
        _systemMenuOperator = system;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Explore:
                return _systemMenuOperator.PlayerIntel.CurrentCoverage == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
            case FleetDirective.Guard:
            case FleetDirective.Patrol:
                // not owned by anyone so no disabling conditions
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _systemMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

