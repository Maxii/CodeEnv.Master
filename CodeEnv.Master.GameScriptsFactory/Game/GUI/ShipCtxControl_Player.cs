// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCtxControl_Player.cs
// Context Menu Control for <see cref="ShipItem"/>s operated by the Human Player.
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
/// Context Menu Control for <see cref="ShipItem"/>s operated by the Human Player.
/// </summary>
public class ShipCtxControl_Player : ACtxControl_Player<ShipDirective> {

    private static ShipDirective[] _selectedItemDirectivesAvailable = new ShipDirective[] {     ShipDirective.Join, 
                                                                                                ShipDirective.Disband, 
                                                                                                ShipDirective.Refit,
                                                                                                ShipDirective.SelfDestruct };

    protected override IEnumerable<ShipDirective> SelectedItemDirectives {
        get { return _selectedItemDirectivesAvailable; }
    }

    protected override int UniqueSubmenuCountReqd { get { return 3; } }

    protected override ADiscernibleItem ItemForFindClosest { get { return _shipMenuOperator; } }
    private ShipItem _shipMenuOperator;

    public ShipCtxControl_Player(ShipItem ship)
        : base(ship.gameObject) {
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
            // TODO
            case ShipDirective.Join:
            case ShipDirective.Disband:
            case ShipDirective.SelfDestruct:
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
                targets = GameObject.FindObjectsOfType<FleetCmdItem>().Where(f => f.Owner.IsPlayer).Except(_shipMenuOperator.Command).Cast<IUnitAttackableTarget>();
                return true;
            case ShipDirective.Refit:
            case ShipDirective.Disband:
                targets = GameObject.FindObjectsOfType<AUnitBaseCmdItem>().Where(b => b.Owner.IsPlayer).Cast<IUnitAttackableTarget>();
                return true;
            case ShipDirective.SelfDestruct:
                targets = Enumerable.Empty<IUnitAttackableTarget>();
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void OnMenuSelection_SelectedItemAccess(int itemID) {
        base.OnMenuSelection_SelectedItemAccess(itemID);

        ShipDirective directive = (ShipDirective)_directiveLookup[itemID];
        IUnitAttackableTarget target;
        bool isTarget = _unitTargetLookup.TryGetValue(itemID, out target);
        string msg = isTarget ? target.FullName : "[none]";
        D.Log("{0} selected directive {1} and target {2} from context menu.", _shipMenuOperator.FullName, directive.GetName(), msg);
        _shipMenuOperator.CurrentOrder = new ShipOrder(directive, OrderSource.Player, target);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

