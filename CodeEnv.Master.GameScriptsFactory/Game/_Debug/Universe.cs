// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Universe.cs
//  Easy access to Universe folder in Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Easy access to Universe folder in Scene.
/// </summary>
public class Universe : AFolderAccess<Universe>, IUniverse {

    /// <summary>
    /// Gets the SpaceTopography value associated with this location in worldspace.
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public SpaceTopography GetSpaceTopography(Vector3 worldLocation) {
        Index3D sectorIndex = SectorGrid.GetSectorIndex(worldLocation);
        SystemModel system;
        if (SystemCreator.TryGetSystem(sectorIndex, out system)) {
            // the sector containing worldLocation has a system
            if (Vector3.SqrMagnitude(worldLocation - system.Position) < system.Radius * system.Radius) {
                return SpaceTopography.System;
            }
        }
        // TODO add Nebula and DeepNebula
        return SpaceTopography.OpenSpace;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

