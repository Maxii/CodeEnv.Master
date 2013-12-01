// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sector.cs
// A placeholder container class for TBD items that will be present in a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// A placeholder container class for TBD items that will be present in a Sector.
/// eg. a nebula particle system.
/// </summary>
public class Sector : AMonoBase {

    public Index3D SectorIndex { get; set; }

    /// <summary>
    /// UNDONE
    /// The density of matter in space in this sector. Intended to be
    /// applied to pathfinding points in the sector as a 'penalty' to
    /// influence path creation. Should also increase drag on a ship
    /// in the sector to reduce its speed for a given thrust. The value
    /// should probably be a function of the OpeYield in the sector.
    /// </summary>
    public float Density { get; set; }

    protected override void Awake() {
        base.Awake();
        Density = 1F;
    }

    /// <summary>
    /// Readonly. Gets the position of this Sector in worldspace.
    /// </summary>
    public Vector3 Position { get { return _transform.position; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

