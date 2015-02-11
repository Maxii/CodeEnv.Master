// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCtxControl_Player.cs
// Context Menu Control for <see cref="FleetCommandItem"/>s operated by the Human Player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="FleetCmdItem"/>s operated by the Human Player.
/// </summary>
public class FleetCtxControl_Player : ACtxControl_Player<FleetDirective> {

    private static FleetDirective[] _menuOperatorDirectivesAvailable = new FleetDirective[] {   FleetDirective.Join,
                                                                                                FleetDirective.Repair, 
                                                                                                FleetDirective.Disband,
                                                                                                FleetDirective.Refit,
                                                                                                FleetDirective.SelfDestruct };

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Join, 
                                                                                                FleetDirective.Move, 
                                                                                                FleetDirective.Guard };

    private static ShipDirective[] _remoteShipDirectivesAvailable = new ShipDirective[] { ShipDirective.Join };

    protected override IEnumerable<FleetDirective> SelectedItemDirectives {
        get { return _menuOperatorDirectivesAvailable; }
    }

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override IEnumerable<ShipDirective> RemoteShipDirectives {
        get { return _remoteShipDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return 4; } }

    protected override ADiscernibleItem ItemForFindClosest { get { return _fleetMenuOperator; } }
    private FleetCmdItem _fleetMenuOperator;

    public FleetCtxControl_Player(FleetCmdItem fleetCmd)
        : base(fleetCmd.gameObject) {
        _fleetMenuOperator = fleetCmd;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_fleetMenuOperator.IsSelected) {
            D.Assert(_fleetMenuOperator == selected as FleetCmdItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsHumanUser;
    }

    protected override bool TryIsRemoteShipAccessAttempted(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = selected as ShipItem;
        return selectedShip != null && selectedShip.Owner.IsHumanUser;
    }

    protected override bool IsSelectedItemMenuItemDisabled(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Repair:
                return _fleetMenuOperator.Data.Health == Constants.OneHundredPercent && _fleetMenuOperator.Data.UnitHealth == Constants.OneHundredPercent;
            case FleetDirective.Join:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
            case FleetDirective.SelfDestruct:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the item associated with this directive can have a submenu and targets, 
    /// <c>false</c> otherwise. Returns the targets for the subMenu if any were found. Default implementation is false and none.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets.</param>
    /// <returns></returns>
    protected override bool TryGetSubMenuUnitTargets_SelectedItemAccess(FleetDirective directive, out IEnumerable<IUnitAttackableTarget> targets) {
        switch (directive) {
            case FleetDirective.Join:
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(b => b.Owner.IsHumanUser).Except(_fleetMenuOperator).Cast<IUnitAttackableTarget>();
                return true;
            case FleetDirective.Repair:
            case FleetDirective.Refit:
            case FleetDirective.Disband:
                targets = GameObject.FindObjectsOfType<AUnitBaseCmdItem>().Where(b => b.Owner.IsHumanUser).Cast<IUnitAttackableTarget>();
                return true;
            case FleetDirective.SelfDestruct:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {  // not really needed
        switch (directive) {
            case FleetDirective.Join:
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteShipMenuItemDisabled(ShipDirective directive) {   // not really needed
        switch (directive) {
            case ShipDirective.Join:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_SelectedItemAccess(int itemID) {
        base.OnMenuSelection_SelectedItemAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _fleetMenuOperator.FullName, directive.GetName(), msg);
        _fleetMenuOperator.CurrentOrder = new FleetOrder(directive, target, Speed.FleetTwoThirds);
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        FleetDirective directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    protected override void OnMenuSelection_RemoteShipAccess(int itemID) {
        base.OnMenuSelection_RemoteShipAccess(itemID);

        var directive = (ShipDirective)_directiveLookup[itemID];
        INavigableTarget target = _fleetMenuOperator;
        var remoteShip = _remotePlayerOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.Player, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

