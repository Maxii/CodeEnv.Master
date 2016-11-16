// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCtxControl_User.cs
// Context Menu Control for <see cref="ShipItem"/>s owned by the User.
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
/// Context Menu Control for <see cref="ShipItem"/>s owned by the User.
/// </summary>
public class ShipCtxControl_User : ACtxControl_User<ShipDirective> {

    private static ShipDirective[] _userMenuOperatorDirectives = new ShipDirective[] {  ShipDirective.Join,
                                                                                        ShipDirective.Disengage,
                                                                                        ShipDirective.Disband,
                                                                                        ShipDirective.Scuttle };
    protected override IEnumerable<ShipDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _shipMenuOperator.Position; } }

    protected override string OperatorName { get { return _shipMenuOperator.FullName; } }

    private bool IsShipDisengageOrderDisabled {
        get {
            return _shipMenuOperator.Data.CombatStance == ShipCombatStance.Disengage ||
                _shipMenuOperator.IsCurrentOrderDirectiveAnyOf(ShipDirective.Disengage) ||
            !_shipMenuOperator.Command.RequestPermissionToWithdraw(_shipMenuOperator, ShipItem.WithdrawPurpose.Disengage);
        }
    }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_User(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenusReqd: 3, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.AssertEqual(_shipMenuOperator, selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(ShipDirective directive) {
        switch (directive) {
            case ShipDirective.Scuttle:
                return _shipMenuOperator.IsCurrentOrderDirectiveAnyOf(directive);
            case ShipDirective.Disengage:
                return IsShipDisengageOrderDisabled;
            case ShipDirective.Join:
            //TODO
            case ShipDirective.Disband:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(ShipDirective directive, out IEnumerable<INavigable> targets) {
        switch (directive) {
            case ShipDirective.Join:
                targets = _userKnowledge.OwnerFleets.Except(_shipMenuOperator.Command).Cast<INavigable>();
                return true;
            case ShipDirective.Disband:
                targets = _userKnowledge.OwnerBases.Cast<INavigable>();
                return true;
            case ShipDirective.Disengage:
            case ShipDirective.Scuttle:
                targets = Enumerable.Empty<INavigable>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _shipMenuOperator.OptimalCameraViewingDistance = _shipMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_UserMenuOperatorIsSelected(itemID);
        IssueUserShipMenuOperatorOrder(itemID);
    }

    private void IssueUserShipMenuOperatorOrder(int itemID) {
        ShipDirective directive = (ShipDirective)_directiveLookup[itemID];
        D.Assert(directive == ShipDirective.Disband || directive == ShipDirective.Join || directive == ShipDirective.Disengage
            || directive == ShipDirective.Scuttle); // HACK
        INavigable target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _shipMenuOperator.FullName, directive.GetValueName(), msg);
        bool toNotifyCmd = false;
        _shipMenuOperator.CurrentOrder = new ShipOrder(directive, OrderSource.User, toNotifyCmd, target as IShipNavigable);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

