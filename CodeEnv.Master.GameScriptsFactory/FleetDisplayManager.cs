// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class FleetDisplayManager : AUnitCmdDisplayManager {

    public new FleetCmdItem Item { get { return base.Item as FleetCmdItem; } }

    protected override float SphericalHighlightRadius { get { return Item.UnitRadius; } }

    public FleetDisplayManager(FleetCmdItem item) : base(item) { }

    protected override Layers GetCullingLayer() { return Layers.ShipCull; }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

