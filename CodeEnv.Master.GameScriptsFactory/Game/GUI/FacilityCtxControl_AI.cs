﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Context Menu Control for <see cref="FacilityItem"/> for AI owned Facilities.
/// </summary>
public class FacilityCtxControl_AI : ACtxControl {

    protected override string OperatorName { get { return _facilityMenuOperator.FullName; } }

    protected override Vector3 PositionForDistanceMeasurements { get { return _facilityMenuOperator.Position; } }

    private FacilityItem _facilityMenuOperator;

    public FacilityCtxControl_AI(FacilityItem facility)
        : base(facility.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _facilityMenuOperator = facility;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_facilityMenuOperator.IsSelected) {
            D.AssertEqual(_facilityMenuOperator, selected as FacilityItem);
            return true;
        }
        return false;
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _facilityMenuOperator.OptimalCameraViewingDistance = _facilityMenuOperator.Position.DistanceToCamera();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

