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
public class Sector : AMonoBehaviourBase {

    public Index3D SectorIndex { get; set; }

    /// <summary>
    /// Readonly. Gets the position of this Sector in worldspace.
    /// </summary>
    public Vector3 Position { get { return _transform.position; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

