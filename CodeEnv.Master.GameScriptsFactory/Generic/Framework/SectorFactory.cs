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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton factory that makes instances of Sectors.
/// </summary>
public class SectorFactory : AGenericSingleton<SectorFactory> {

    private GameObject _sectorPrefab;
    private GameObject _sectorFolder;

    private SectorFactory() {
        Initialize();
    }

    protected override void Initialize() {
        _sectorFolder = SectorsFolder.Instance.gameObject;
        _sectorPrefab = RequiredPrefabs.Instance.sector.gameObject;
    }

    public SectorItem MakeInstance(Index3D sectorIndex, Vector3 worldLocation) {
        GameObject sectorGO = NGUITools.AddChild(_sectorFolder, _sectorPrefab);
        // sector.Awake() runs immediately here, then disables itself
        SectorItem sector = sectorGO.GetSafeMonoBehaviourComponent<SectorItem>();

        SectorData data = new SectorData(sectorIndex) {
            Density = 1F
        };
        sector.Data = data;
        // IMPROVE use data values in place of sector values

        sectorGO.transform.position = worldLocation;
        sector.enabled = true;
        return sector;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


