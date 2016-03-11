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
                                                                                        ShipDirective.Withdraw,
                                                                                        ShipDirective.Disband,
                                                                                        ShipDirective.Scuttle };
    protected override IEnumerable<ShipDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _shipMenuOperator; } }

    protected override string OperatorName { get { return _shipMenuOperator.FullName; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_User(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenusReqd: 3, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.Assert(_shipMenuOperator == selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(ShipDirective directive) {
        switch (directive) {
            case ShipDirective.Join:
            case ShipDirective.Withdraw:
            //TODO
            case ShipDirective.Disband:
            case ShipDirective.Scuttle:
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
    protected override bool TryGetSubMenuUnitTargets_MenuOperatorIsSelected(ShipDirective directive, out IEnumerable<INavigableTarget> targets) {
        switch (directive) {
            case ShipDirective.Join:
                targets = _userKnowledge.MyFleets.Except(_shipMenuOperator.Command).Cast<INavigableTarget>();
                return true;
            case ShipDirective.Disband:
                targets = _userKnowledge.MyBases.Cast<INavigableTarget>();
                return true;
            case ShipDirective.Withdraw:
            case ShipDirective.Scuttle:
                targets = Enumerable.Empty<INavigableTarget>();
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
        IssueShipMenuOperatorOrder(itemID);
    }

    private void IssueShipMenuOperatorOrder(int itemID) {
        ShipDirective directive = (ShipDirective)_directiveLookup[itemID];
        INavigableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _shipMenuOperator.FullName, directive.GetValueName(), msg);
        _shipMenuOperator.CurrentOrder = new ShipOrder(directive, OrderSource.User, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

