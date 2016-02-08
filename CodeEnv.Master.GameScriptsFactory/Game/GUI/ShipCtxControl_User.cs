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

    private static ShipDirective[] _selectedItemDirectivesAvailable = new ShipDirective[] {     ShipDirective.Join,
                                                                                                ShipDirective.Disband,
                                                                                                ShipDirective.Refit,
                                                                                                ShipDirective.Scuttle };
    protected override IEnumerable<ShipDirective> SelectedItemDirectives {
        get { return _selectedItemDirectivesAvailable; }
    }

    protected override ADiscernibleItem ItemForFindClosest { get { return _shipMenuOperator; } }

    protected override string OperatorName { get { return _shipMenuOperator.FullName; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_User(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenusReqd: 3, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
    }

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.Assert(_shipMenuOperator == selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override bool IsSelectedItemMenuItemDisabled(ShipDirective directive) {
        switch (directive) {
            case ShipDirective.Refit:
            //TODO
            case ShipDirective.Join:
            case ShipDirective.Disband:
            case ShipDirective.Scuttle:
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
    protected override bool TryGetSubMenuUnitTargets_SelectedItemAccess(ShipDirective directive, out IEnumerable<IUnitAttackableTarget> targets) {
        switch (directive) {
            case ShipDirective.Join:
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(f => f.Owner.IsUser).Except(_shipMenuOperator.Command).Cast<IUnitAttackableTarget>();
                return true;
            case ShipDirective.Refit:
            case ShipDirective.Disband:
                targets = GameObject.FindObjectsOfType<AUnitBaseCmdItem>().Where(b => b.Owner.IsUser).Cast<IUnitAttackableTarget>();
                return true;
            case ShipDirective.Scuttle:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuSelection_OptimalFocusDistance() {
        _shipMenuOperator.OptimalCameraViewingDistance = _shipMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuSelection_SelectedItemAccess(int itemID) {
        base.HandleMenuSelection_SelectedItemAccess(itemID);

        ShipDirective directive = (ShipDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _shipMenuOperator.FullName, directive.GetValueName(), msg);
        _shipMenuOperator.CurrentOrder = new ShipOrder(directive, OrderSource.UnitCommand, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

