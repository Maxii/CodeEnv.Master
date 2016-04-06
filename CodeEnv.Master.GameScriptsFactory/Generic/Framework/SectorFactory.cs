// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorFactory.cs
// Singleton factory that makes instances of Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton factory that makes instances of Sectors.
/// </summary>
public class SectorFactory : AGenericSingleton<SectorFactory> {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private const string SectorNameFormat = "Sector {0}";

    private GameObject _sectorPrefab;

    private SectorFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        _sectorPrefab = RequiredPrefabs.Instance.sector.gameObject;
    }

    public SectorItem MakeSectorInstance(Index3D sectorIndex, Vector3 worldLocation) {
        GameObject sectorGO = UnityUtility.AddChild(SectorsFolder.Instance.Folder.gameObject, _sectorPrefab);
        // sector.Awake() runs immediately here, then disables itself
        SectorItem sector = sectorGO.GetSafeComponent<SectorItem>();

        sector.Name = SectorNameFormat.Inject(sectorIndex);
        SectorData data = new SectorData(sector, sectorIndex) {
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        // IMPROVE use data values in place of sector values

        sectorGO.transform.position = worldLocation;
        // sector will be enabled when Sector.CommenceOperations() called
        return sector;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


