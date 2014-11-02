﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl_Player.cs
// Context Menu Control for <see cref="SystemItem"/>s operated by the Human Player.
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
/// Context Menu Control for <see cref="SystemItem"/>s operated by the Human Player.
/// </summary>
public class SystemCtxControl_Player : ACtxControl_Player<BaseDirective> {

    private static BaseDirective[] _selectedItemDirectivesAvailable = new BaseDirective[] {     BaseDirective.Repair, 
                                                                                                BaseDirective.Refit, 
                                                                                                BaseDirective.Attack };

    private static FleetDirective[] _remoteFleetDirectivesAvailable = new FleetDirective[] {    FleetDirective.Disband, 
                                                                                                FleetDirective.Refit,
                                                                                                FleetDirective.Repair,
                                                                                                FleetDirective.Move, 
                                                                                                FleetDirective.Guard };

    private static ShipDirective[] _remoteShipDirectivesAvailable = new ShipDirective[] {   ShipDirective.Disband, 
                                                                                            ShipDirective.Refit };

    protected override IEnumerable<BaseDirective> SelectedItemDirectives {
        get { return _selectedItemDirectivesAvailable; }
    }

    protected override IEnumerable<FleetDirective> RemoteFleetDirectives {
        get { return _remoteFleetDirectivesAvailable; }
    }

    protected override IEnumerable<ShipDirective> RemoteShipDirectives {
        get { return _remoteShipDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return 1; } }

    protected override AItem ItemForFindClosest { get { return _settlement; } }
    private SystemItem _systemMenuOperator;
    private SettlementCommandItem _settlement;

    public SystemCtxControl_Player(SystemItem system)
        : base(system.gameObject) {
        _systemMenuOperator = system;
        _settlement = system.Settlement;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_systemMenuOperator.IsSelected) {
            D.Assert(_systemMenuOperator == selected as SystemItem);
            return true;
        }
        return false;
    }

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCommandItem selectedFleet) {
        selectedFleet = selected as FleetCommandItem;
        return selectedFleet != null && selectedFleet.Owner.IsPlayer;
    }

    protected override bool TryIsRemoteShipAccessAttempted(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = selected as ShipItem;
        return selectedShip != null && selectedShip.Owner.IsPlayer;
    }

    protected override bool IsSelectedItemMenuItemDisabled(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Repair:
                return _settlement.Data.Health == Constants.OneHundredPercent && _settlement.Data.UnitHealth == Constants.OneHundredPercent;
            case BaseDirective.Refit:
            // TODO 
            case BaseDirective.Attack:
                // TODO
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
    protected override bool TryGetSubMenuUnitTargets_SelectedItemAccess(BaseDirective directive, out IEnumerable<IUnitTarget> targets) {
        switch (directive) {
            case BaseDirective.Attack:
                targets = GameObject.FindObjectsOfType<FleetCommandItem>().Where(f => f.Owner.IsEnemyOf(_systemMenuOperator.Owner)).Cast<IUnitTarget>();
                return true;
            case BaseDirective.Repair:
            case BaseDirective.Refit:
                targets = Enumerable.Empty<IUnitTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {  // not really needed
        switch (directive) {
            case FleetDirective.Repair:
                var fleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
                return fleet.Data.UnitHealth == Constants.OneHundredPercent && fleet.Data.Health == Constants.OneHundredPercent;
            case FleetDirective.Disband:
            case FleetDirective.Refit:
            case FleetDirective.Move:
            case FleetDirective.Guard:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteShipMenuItemDisabled(ShipDirective directive) {   // not really needed
        switch (directive) {
            case ShipDirective.Disband:
            case ShipDirective.Refit:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_SelectedItemAccess(int itemID) {
        base.OnMenuSelection_SelectedItemAccess(itemID);

        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _systemMenuOperator.FullName, directive.GetName(), msg);
        _settlement.CurrentOrder = new BaseOrder(directive, target);
    }

    protected override void OnMenuSelection_RemoteFleetAccess(int itemID) {
        base.OnMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        IDestinationTarget target = directive.EqualsAnyOf(FleetDirective.Disband, FleetDirective.Refit, FleetDirective.Repair) ? _settlement as IDestinationTarget : _systemMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCommandItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    protected override void OnMenuSelection_RemoteShipAccess(int itemID) {
        base.OnMenuSelection_RemoteShipAccess(itemID);

        var directive = (ShipDirective)_directiveLookup[itemID];
        IDestinationTarget target = _settlement;
        var remoteShip = _remotePlayerOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.Player, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
