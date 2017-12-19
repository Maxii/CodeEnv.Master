// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityCtxControl_AI.cs
// Context Menu Control for <see cref="FacilityItem"/> for AI owned Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="FacilityItem"/> for AI owned Facilities.
/// </summary>
public class FacilityCtxControl_AI : ACtxControl {

    protected override string OperatorName { get { return _facilityMenuOperator != null ? _facilityMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override Vector3 PositionForDistanceMeasurements { get { return _facilityMenuOperator.Position; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _facilityMenuOperator.IsFocus; } }

    private FacilityItem _facilityMenuOperator;

    public FacilityCtxControl_AI(FacilityItem facility)
        : base(facility.gameObject, uniqueSubmenuQtyReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _facilityMenuOperator = facility;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_facilityMenuOperator.IsSelected) {
            D.AssertEqual(_facilityMenuOperator, selected as FacilityItem);
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
        _facilityMenuOperator.__ChangeOwner(_user);
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _facilityMenuOperator.OptimalCameraViewingDistance = _facilityMenuOperator.Position.DistanceToCamera();
    }

    #region Debug

    private void __PopulateChgOwnerMenu() {
        _ctxObject.menuItems = new CtxMenu.Item[] { new CtxMenu.Item() {
            text = "ChgOwner",
            id = Constants.MinusOne,
            isDisabled = !_facilityMenuOperator.IsAssaultAllowedBy(_user)
        }};
    }

    #endregion

}

