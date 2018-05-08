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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
/// <remarks>12.13.17 Removed Disband as ship doesn't know whether it is to split from fleet.</remarks>
/// </summary>
public class ShipCtxControl_User : ACtxControl_User<ShipDirective> {

    private static ShipDirective[] _userMenuOperatorDirectives = new ShipDirective[]    {
                                                                                            ShipDirective.JoinFleetShortcut,
                                                                                            ShipDirective.JoinHangerShortcut,
                                                                                            ShipDirective.Disengage,
                                                                                            ShipDirective.Scuttle
                                                                                        };

    protected override IEnumerable<ShipDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _shipMenuOperator.Position; } }

    protected override string OperatorName { get { return _shipMenuOperator != null ? _shipMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _shipMenuOperator.IsFocus; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_User(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenusReqd: 1, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
        __ValidateUniqueSubmenuQtyReqd();
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.AssertEqual(_shipMenuOperator, selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(ShipDirective directive) {
        // IsCurrentOrderDirectiveAnyOf() not used in criteria as target in current order may not be the same
        switch (directive) {
            case ShipDirective.Scuttle:
            case ShipDirective.Disengage:
            case ShipDirective.JoinFleetShortcut:
            case ShipDirective.JoinHangerShortcut:
                // 12.5.17 Currently no CtxMenu orders allowed for ships in hangers
                return _shipMenuOperator.IsLocatedInHanger || !_shipMenuOperator.IsAuthorizedForNewOrder(directive);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// <remarks>The return value answers the question "Does the directive support submenus?" It does not mean "Are there any targets
    /// in the submenu?" so don't return targets.Any()!</remarks>
    /// <remarks>12.13.17 Avoid asserting anything here based off of the assumption that IsUserMenuOperatorMenuItemDisabledFor will 
    /// have already disabled the selection if the assert would fail. This method is also used to count the number of reqd submenus.</remarks>
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(ShipDirective directive, out IEnumerable<INavigableDestination> targets) {
        bool doesDirectiveSupportSubmenus = false;
        switch (directive) {
            case ShipDirective.JoinFleetShortcut:
                IEnumerable<IFleetCmd> joinableFleets;
                _userKnowledge.TryGetJoinableFleetsFor(_shipMenuOperator, out joinableFleets);
                targets = joinableFleets.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case ShipDirective.JoinHangerShortcut:
                IEnumerable<IUnitBaseCmd> joinableHangerBases;
                _userKnowledge.TryGetJoinableHangerBasesFor(_shipMenuOperator, out joinableHangerBases);
                targets = joinableHangerBases.Cast<INavigableDestination>();
                doesDirectiveSupportSubmenus = true;
                break;
            case ShipDirective.Disengage:
            case ShipDirective.Scuttle:
                targets = Enumerable.Empty<INavigableDestination>();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
        return doesDirectiveSupportSubmenus;
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _shipMenuOperator.OptimalCameraViewingDistance = _shipMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        IssueUserShipMenuOperatorOrder(itemID);
    }

    private void IssueUserShipMenuOperatorOrder(int itemID) {
        ShipDirective directive = (ShipDirective)_directiveLookup[itemID];
        INavigableDestination submenuTgt;
        bool hasSubmenuTgt = _unitSubmenuTgtLookup.TryGetValue(itemID, out submenuTgt);
        string submenuTgtMsg = hasSubmenuTgt ? submenuTgt.DebugName : "[none]";
        D.LogBold("{0} selected directive {1} and submenu target {2} from context menu.", DebugName, directive.GetValueName(), submenuTgtMsg);
        _shipMenuOperator.CurrentOrder = new ShipOrder(directive, OrderSource.User, submenuTgt as IShipNavigableDestination);
    }

}

