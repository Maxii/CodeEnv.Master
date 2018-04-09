// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityHull.cs
// The hull of a facility. 
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
/// The hull of a facility. 
/// </summary>
public class FacilityHull : AHull, IFacilityHull {

    private const string DebugNameFormat = "{0}[{1}] MaxTurretWeaps: {2}, MaxSiloWeaps: {3}.";

    [SerializeField]
    private FacilityHullCategory _hullCategory = FacilityHullCategory.None;

    public FacilityHullCategory HullCategory { get { return _hullCategory; } }

    public override string DebugName {
        get { return DebugNameFormat.Inject(base.DebugName, HullCategory.GetValueName(), MaxAllowedLosWeapons, MaxAllowedLaunchedWeapons); }
    }

    protected override int MaxAllowedLosWeapons { get { return _hullCategory.MaxTurretMounts(); } }

    protected override int MaxAllowedLaunchedWeapons { get { return _hullCategory.MaxSiloMounts(); } }

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotDefault((int)_hullCategory);
    }

    protected override void Cleanup() { }


}

