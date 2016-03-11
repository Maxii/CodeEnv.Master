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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Context Menu Control for <see cref="FacilityItem"/>s owned by the User.
/// </summary>
public class FacilityCtxControl_User : ACtxControl_User<FacilityDirective> {

    private static FacilityDirective[] _userMenuOperatorDirectives = new FacilityDirective[] {  FacilityDirective.Disband,
                                                                                                FacilityDirective.Scuttle };
    protected override IEnumerable<FacilityDirective> UserMenuOperatorDirectives {
        get { return _userMenuOperatorDirectives; }
    }

    protected override AItem ItemForDistanceMeasurements { get { return _facilityMenuOperator; } }

    protected override string OperatorName { get { return _facilityMenuOperator.FullName; } }

    private FacilityItem _facilityMenuOperator;

    public FacilityCtxControl_User(FacilityItem facility)
    : base(facility.gameObject, uniqueSubmenusReqd: 0, menuPosition: MenuPositionMode.Offset) {
        _facilityMenuOperator = facility;
    }

    protected override bool TryIsSelectedItemMenuOperator(ISelectable selected) {
        if (_facilityMenuOperator.IsSelected) {
            D.Assert(_facilityMenuOperator == selected as FacilityItem);
            return true;
        }
        return false;
    }

    protected override bool IsUserMenuOperatorMenuItemDisabledFor(FacilityDirective directive) {
        switch (directive) {
            case FacilityDirective.Disband:
            case FacilityDirective.Scuttle:
                return false;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _facilityMenuOperator.OptimalCameraViewingDistance = _facilityMenuOperator.Position.DistanceToCamera();
    }

    protected override void HandleMenuPick_UserMenuOperatorIsSelected(int itemID) {
        base.HandleMenuPick_UserMenuOperatorIsSelected(itemID);
        IssueFacilityMenuOperatorOrder(itemID);
    }

    private void IssueFacilityMenuOperatorOrder(int itemID) {
        FacilityDirective directive = (FacilityDirective)_directiveLookup[itemID];
        D.Log("{0} selected directive {1} from context menu.", _facilityMenuOperator.FullName, directive.GetValueName());
        _facilityMenuOperator.CurrentOrder = new FacilityOrder(directive, OrderSource.User);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

