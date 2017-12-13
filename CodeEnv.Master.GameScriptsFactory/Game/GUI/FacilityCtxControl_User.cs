// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityCtxControl_User.cs
// Context Menu Control for <see cref="FacilityItem"/>s owned by the User.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="FacilityItem"/>s owned by the User.
/// </summary>
public class FacilityCtxControl_User : ACtxControl_User<FacilityDirective> {

    private static FacilityDirective[] _userMenuOperatorDirectives = new FacilityDirective[]    {
                                                                                                    FacilityDirective.Disband,
                                                                                                    FacilityDirective.Scuttle,
                                                                                                };

    protected override IEnumerable<FacilityDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override Vector3 PositionForDistanceMeasurements { get { return _facilityMenuOperator.Position; } }

    protected override string OperatorName { get { return _facilityMenuOperator != null ? _facilityMenuOperator.DebugName : "NotYetAssigned"; } }

    protected override bool IsItemMenuOperatorTheCameraFocus { get { return _facilityMenuOperator.IsFocus; } }

    private FacilityItem _facilityMenuOperator;

    public FacilityCtxControl_User(FacilityItem facility)
        : base(facility.gameObject, uniqueSubmenusReqd: 0, menuPosition: MenuPositionMode.Offset) {
        _facilityMenuOperator = facility;
        __ValidateUniqueSubmenuQtyReqd();
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_facilityMenuOperator.IsSelected) {
            D.AssertEqual(_facilityMenuOperator, selected as FacilityItem);
            return true;
        }
        return false;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(FacilityDirective directive) {
        switch (directive) {
            case FacilityDirective.Disband:
            // IsCurrentOrderDirectiveAnyOf not needed as IsAuthorizedForNewOrder will correctly respond once order processed
            ////return !_facilityMenuOperator.IsAuthorizedForNewOrder(directive); //// || _facilityMenuOperator.IsCurrentOrderDirectiveAnyOf(directive);
            case FacilityDirective.Scuttle:
                return !_facilityMenuOperator.IsAuthorizedForNewOrder(directive);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _facilityMenuOperator.OptimalCameraViewingDistance = _facilityMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_MenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_MenuOperatorIsSelected(itemID);
        IssueUserFacilityMenuOperatorOrder(itemID);
    }

    private void IssueUserFacilityMenuOperatorOrder(int itemID) {
        FacilityDirective directive = (FacilityDirective)_directiveLookup[itemID];
        D.Log("{0} selected directive {1} from context menu.", DebugName, directive.GetValueName());
        _facilityMenuOperator.CurrentOrder = new FacilityOrder(directive, OrderSource.User);
    }

}

