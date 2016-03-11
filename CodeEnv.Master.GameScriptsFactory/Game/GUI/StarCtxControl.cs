// --------------------------------------------------------------------------------------------------------------------
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

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.Move,
                                                                                        FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Patrol,
                                                                                        FleetDirective.Guard,
                                                                                        FleetDirective.Explore };
    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _starMenuOperator; } }

    protected override string OperatorName { get { return _starMenuOperator.FullName; } }

    private StarItem _starMenuOperator;

    public StarCtxControl(StarItem star)
        : base(star.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _starMenuOperator = star;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_starMenuOperator.IsSelected) {
            D.Assert(_starMenuOperator == selected as StarItem);
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
            case FleetDirective.Explore:
                // A fleet may explore a star (system) if not at war and not already explored
                return (_starMenuOperator.System as IFleetExplorable).IsFullyExploredBy(_user) ||
                    _starMenuOperator.Owner.IsAtWarWith(_user);
            case FleetDirective.Patrol:
            // A fleet may patrol any star (system) without regard to Diplo state
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                // A fleet may move to any star without regard to Diplo state
                return false;
            case FleetDirective.Guard:
                return _user.IsEnemyOf(_starMenuOperator.Owner);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _starMenuOperator.OptimalCameraViewingDistance = _starMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserRemoteFleetIsSelected(int itemID) {
        base.HandleMenuPick_UserRemoteFleetIsSelected(itemID);
        IssueRemoteFleetOrder(itemID);
    }

    private void IssueRemoteFleetOrder(int itemID) {
        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _starMenuOperator;
        if (directive == FleetDirective.Explore) {
            target = _starMenuOperator.System as INavigableTarget;
        }
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

