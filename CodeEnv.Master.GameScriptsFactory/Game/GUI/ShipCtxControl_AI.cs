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

    protected override string OperatorName { get { return _shipMenuOperator.FullName; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_AI(ShipItem ship)
        : base(ship.gameObject, uniqueSubmenusReqd: Constants.Zero, menuPosition: MenuPositionMode.Offset) {
        _shipMenuOperator = ship;
    }

    protected override bool IsSelectedItemMenuOperator(ISelectable selected) {
        if (_shipMenuOperator.IsSelected) {
            D.AssertEqual(_shipMenuOperator, selected as ShipItem);
            return true;
        }
        return false;
    }

    protected override void HandleMenuPick_OptimalFocusDistance() {
        _shipMenuOperator.OptimalCameraViewingDistance = _shipMenuOperator.Position.DistanceToCamera();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

