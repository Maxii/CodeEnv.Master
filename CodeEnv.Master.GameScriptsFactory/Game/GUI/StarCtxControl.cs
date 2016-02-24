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

    private static IDictionary<FleetDirective, Speed> _userFleetSpeedLookup = new Dictionary<FleetDirective, Speed>() {
        {FleetDirective.Move, Speed.FleetStandard },
        {FleetDirective.Guard, Speed.FleetStandard },
        {FleetDirective.Explore, Speed.FleetTwoThirds },
        {FleetDirective.Patrol, Speed.FleetOneThird }
    };

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] {     FleetDirective.Move,
                                                                                            FleetDirective.Patrol,
                                                                                            FleetDirective.Guard,
                                                                                            FleetDirective.Explore };
    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives {
        get { return _userRemoteFleetDirectives; }
    }

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
                // IMPROVE exploring a star is always available? needed to initiate explore of unknown system?
                return false;
            case FleetDirective.Patrol:
            case FleetDirective.Move:
                return false;
            case FleetDirective.Guard:
                return _remoteUserOwnedSelectedItem.Owner.IsEnemyOf(_starMenuOperator.Owner);
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
        Speed speed = _userFleetSpeedLookup[directive];
        INavigableTarget target = _starMenuOperator;
        var remoteFleet = _remoteUserOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, OrderSource.User, target, speed);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

