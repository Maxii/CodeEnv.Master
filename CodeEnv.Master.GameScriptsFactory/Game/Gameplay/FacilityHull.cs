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

    [SerializeField]
    private FacilityHullCategory _hullCategory = FacilityHullCategory.None;

    public FacilityHullCategory HullCategory { get { return _hullCategory; } }

    protected override int MaxAllowedLosWeapons { get { return _hullCategory.__MaxLOSWeapons(); } }

    protected override int MaxAllowedMissileWeapons { get { return _hullCategory.__MaxMissileWeapons(); } }

    protected override void Validate() {
        base.Validate();
        D.AssertNotDefault((int)_hullCategory);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

