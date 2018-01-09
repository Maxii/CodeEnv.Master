// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCtxControl_AI.cs
// Context Menu Control for <see cref="ShipItem"/>s owned by the AI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="ShipItem"/>s owned by the AI.
/// </summary>
public class ShipCtxControl_AI : ACtxControl {

    protected override Vector3 PositionForDistanceMeasurements { get { return _shipMenuOperator.Position; } }

    protected override string OperatorName { get { return _shipMenuOperator != null ? _shipMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _shipMenuOperator.IsFocus; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_AI(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.AssertEqual(_shipMenuOperator, selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override void PopulateMenu_MenuOperatorIsSelected() {
        base.PopulateMenu_MenuOperatorIsSelected();
        __PopulateChgOwnerMenu();
    }

    protected override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        D.AssertEqual(Constants.MinusOne, itemID);
        ShipOrder chgOwnerOrder = new ShipOrder(ShipDirective.__ChgOwner, OrderSource.User);
        _shipMenuOperator.CurrentOrder = chgOwnerOrder;
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _shipMenuOperator.OptimalCameraViewingDistance = _shipMenuOperator.Position.DistanceToCamera();
    }

    #region Debug

    private void __PopulateChgOwnerMenu() {
        _ctxObject.menuItems = new CtxMenu.Item[] { new CtxMenu.Item() {
            text = "__ChgOwner",
            id = Constants.MinusOne,
            isDisabled = !_shipMenuOperator.IsAssaultAllowedBy(_user)
        }};
    }

    #endregion

}

