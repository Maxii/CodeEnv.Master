﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorModel.cs
// A placeholder container class for TBD items that will be present in a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A placeholder container class for TBD items that will be present in a Sector.
/// eg. a nebula particle system.
/// </summary>
public class SectorModel : AItemModel {

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex {
        get { return Data.SectorIndex; }
    }

    /// <summary>
    /// Readonly. Gets the position of this Sector in worldspace.
    /// </summary>
    public Vector3 Position { get { return Data.Position; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

