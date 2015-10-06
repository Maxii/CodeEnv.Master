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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The hull of a ship.
/// </summary>
public class ShipHull : AHull, IShipHull {

    public Transform[] engineNozzles;

    public ShipHullCategory hullCategory;

    public ShipHullCategory HullCategory { get { return hullCategory; } }

    protected override int MaxAllowedLosWeapons { get { return hullCategory.__MaxLOSWeapons(); } }

    protected override int MaxAllowedMissileWeapons { get { return hullCategory.__MaxMissileWeapons(); } }

    protected override void Validate() {
        base.Validate();
        //D.Assert(!engineNozzles.IsNullOrEmpty());
        D.Assert(hullCategory != ShipHullCategory.None);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

