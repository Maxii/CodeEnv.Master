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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A placeholder container class for TBD items that will be present in a Sector.
/// eg. a nebula particle system.
/// </summary>
public class SectorModel : AOwnedItemModel, INavigableTarget {

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex {
        get { return Data.SectorIndex; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        Radius = TempGameValues.SectorSideLength / 2F;  // the radius of the sphere inscribed inside a sector box
        // there is no collider associated with a SectorModel. The collider used for context menu activation is part of the SectorExaminer
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public Topography Topography { get { return Data.Topography; } }

    #endregion


}

