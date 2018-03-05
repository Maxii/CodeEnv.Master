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

    private const string DebugNameFormat = "{0}[{1}] MaxTurretWeaps: {2}, MaxSiloWeaps: {3}.";

    //[SerializeField]
    public Transform[] engineNozzles;

    //[FormerlySerializedAs("hullCategory")]
    [SerializeField]
    private ShipHullCategory _hullCategory = ShipHullCategory.None;

    public ShipHullCategory HullCategory { get { return _hullCategory; } }

    public override string DebugName {
        get { return DebugNameFormat.Inject(base.DebugName, HullCategory.GetValueName(), MaxAllowedLosWeapons, MaxAllowedLaunchedWeapons); }
    }

    protected override int MaxAllowedLosWeapons { get { return _hullCategory.MaxTurretMounts(); } }

    protected override int MaxAllowedLaunchedWeapons { get { return _hullCategory.MaxSiloMounts(); } }

    protected override void Validate() {
        base.Validate();
        //D.Assert(!_engineNozzles.IsNullOrEmpty());
        D.AssertNotDefault((int)_hullCategory);
    }

    protected override void Cleanup() { }


}

