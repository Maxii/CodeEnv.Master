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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The hull of a facility. 
/// </summary>
public class FacilityHull : AHull, IFacilityHull {

    public FacilityHullCategory hullCategory;

    public FacilityHullCategory HullCategory { get { return hullCategory; } }

    protected override int MaxAllowedLosWeapons { get { return hullCategory.__MaxLOSWeapons(); } }

    protected override int MaxAllowedMissileWeapons { get { return hullCategory.__MaxMissileWeapons(); } }

    protected override void Validate() {
        base.Validate();
        D.Assert(hullCategory != FacilityHullCategory.None);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

