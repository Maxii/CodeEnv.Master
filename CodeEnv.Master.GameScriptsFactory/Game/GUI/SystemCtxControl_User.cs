// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl_User.cs
// Context Menu Control for <see cref="SystemItem"/>s owned by the User.
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
/// Context Menu Control for <see cref="SystemItem"/>s owned by the User.
/// </summary>
public class SystemCtxControl_User : ACtxControl_User<BaseDirective> {

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

    protected override ADiscernibleItem ItemForFindClosest { get { return _settlement; } }
    private SystemItem _systemMenuOperator;
    private SettlementCmdItem _settlement;

    public SystemCtxControl_User(SystemItem system)
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

    protected override bool TryIsRemoteFleetAccessAttempted(ISelectable selected, out FleetCmdItem selectedFleet) {
        selectedFleet = selected as FleetCmdItem;
        return selectedFleet != null && selectedFleet.Owner.IsUser;
    }

    protected override bool TryIsRemoteShipAccessAttempted(ISelectable selected, out ShipItem selectedShip) {
        selectedShip = selected as ShipItem;
        return selectedShip != null && selectedShip.Owner.IsUser;
    }

    protected override bool IsSelectedItemMenuItemDisabled(BaseDirective directive) {
        switch (directive) {
            case BaseDirective.Repair:
                return _settlement.Data.Health == Constants.OneHundredPercent && _settlement.Data.UnitHealth == Constants.OneHundredPercent;
            case BaseDirective.Refit:
            //TODO 
            case BaseDirective.Attack:
                //TODO
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
    protected override bool TryGetSubMenuUnitTargets_SelectedItemAccess(BaseDirective directive, out IEnumerable<IUnitAttackableTarget> targets) {
        switch (directive) {
            case BaseDirective.Attack:
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(f => f.Owner.IsEnemyOf(_systemMenuOperator.Owner)).Cast<IUnitAttackableTarget>();
                return true;
            case BaseDirective.Repair:
            case BaseDirective.Refit:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override bool IsRemoteFleetMenuItemDisabled(FleetDirective directive) {  // not really needed
        switch (directive) {
            case FleetDirective.Repair:
                var fleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
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

    protected override void HandleMenuSelection_SelectedItemAccess(int itemID) {
        base.HandleMenuSelection_SelectedItemAccess(itemID);

        BaseDirective directive = (BaseDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _systemMenuOperator.FullName, directive.GetValueName(), msg);
        _settlement.CurrentOrder = new BaseOrder(directive, target);
    }

    protected override void HandleMenuSelection_RemoteFleetAccess(int itemID) {
        base.HandleMenuSelection_RemoteFleetAccess(itemID);

        var directive = (FleetDirective)_directiveLookup[itemID];
        INavigableTarget target = directive.EqualsAnyOf(FleetDirective.Disband, FleetDirective.Refit, FleetDirective.Repair) ? _settlement as INavigableTarget : _systemMenuOperator;
        var remoteFleet = _remotePlayerOwnedSelectedItem as FleetCmdItem;
        remoteFleet.CurrentOrder = new FleetOrder(directive, target, Speed.FleetStandard);
    }

    protected override void HandleMenuSelection_RemoteShipAccess(int itemID) {
        base.HandleMenuSelection_RemoteShipAccess(itemID);

        var directive = (ShipDirective)_directiveLookup[itemID];
        INavigableTarget target = _settlement;
        var remoteShip = _remotePlayerOwnedSelectedItem as ShipItem;
        remoteShip.CurrentOrder = new ShipOrder(directive, OrderSource.UnitCommand, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

