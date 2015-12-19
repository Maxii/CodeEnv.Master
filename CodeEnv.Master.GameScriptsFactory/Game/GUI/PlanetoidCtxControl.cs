// --------------------------------------------------------------------------------------------------------------------
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
/// No distinction between AI and User owned.
/// </summary>
public class PlanetoidCtxControl : ACtxControl {

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Move,
                                                                                                FleetDirective.Attack,
                                                                                                FleetDirective.Guard,
                                                                                                FleetDirective.Explore };
    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override string OperatorName { get { return _planetoidMenuOperator.FullName; } }

    private APlanetoidItem _planetoidMenuOperator;

    public PlanetoidCtxControl(APlanetoidItem planetoid)
        : base(planetoid.gameObject, uniqueSubmenusReqd: Constants.Zero, toOffsetMenu: true) {
        _planetoidMenuOperator = planetoid;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_planetoidMenuOperator.IsSelected) {
            D.Assert(_planetoidMenuOperator == selected as APlanetoidItem);
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
                return !_remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_planetoidMenuOperator.Owner);
            case FleetDirective.Explore:
                return _planetoidMenuOperator.GetUserIntelCoverage() == IntelCoverage.Comprehensive;
            case FleetDirective.Move:
                return false;
            case FleetDirective.Guard:
                return _remotePlayerOwnedSelectedItem.Owner.IsEnemyOf(_planetoidMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuSelection_OptimalFocusDistance() {
        _planetoidMenuOperator.OptimalCameraViewingDistance = _planetoidMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuSelection_RemoteFleetAccess(int itemID) {
        base.HandleMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _planetoidMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

