// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterCtxControl.cs
// Context Menu Control for <see cref="UniverseCenterItem"/>.
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
/// Context Menu Control for <see cref="UniverseCenterItem"/>.
/// No distinction between AI and User owned.    
/// </summary>
public class UniverseCenterCtxControl : ACtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Patrol,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.Explore};
    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _universeCenterMenuOperator; } }

    protected override string OperatorName { get { return _universeCenterMenuOperator.FullName; } }

    private UniverseCenterItem _universeCenterMenuOperator;

    public UniverseCenterCtxControl(UniverseCenterItem universeCenter)
        : base(universeCenter.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _universeCenterMenuOperator = universeCenter;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_universeCenterMenuOperator.IsSelected) {
            D.Assert(_universeCenterMenuOperator == selected as UniverseCenterItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsSelectedItemUserRemoteFleet(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Patrol:
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
            case FleetDirective.Guard:
                return false;
            case FleetDirective.Explore:
                return (_universeCenterMenuOperator as IFleetExplorable).IsFullyExploredBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _universeCenterMenuOperator.OptimalCameraViewingDistance = _universeCenterMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _universeCenterMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

