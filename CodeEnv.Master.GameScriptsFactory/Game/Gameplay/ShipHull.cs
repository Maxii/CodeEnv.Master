// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHull.cs
// The hull of a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// The hull of a ship.
/// </summary>
public class ShipHull : AHull, IShipHull {

    //[SerializeField]
    public Transform[] engineNozzles;

    //[FormerlySerializedAs("hullCategory")]
    [SerializeField]
    private ShipHullCategory _hullCategory = ShipHullCategory.None;

    public ShipHullCategory HullCategory { get { return _hullCategory; } }

    protected override int MaxAllowedLosWeapons { get { return _hullCategory.__MaxLOSWeapons(); } }

    protected override int MaxAllowedLaunchedWeapons { get { return _hullCategory.__MaxLaunchedWeapons(); } }

    protected override void Validate() {
        base.Validate();
        //D.Assert(!_engineNozzles.IsNullOrEmpty());
        D.AssertNotDefault((int)_hullCategory);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

