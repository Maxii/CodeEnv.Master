// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RequiredPrefabs.cs
// Singleton container that holds prefabs that are guaranteed to be used in the GameScene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton container that holds prefabs that are guaranteed to be used in the GameScene.
/// </summary>
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the startScene. As such, they must be Instantiated before use.
/// </remarks>
public class RequiredPrefabs : AMonoSingleton<RequiredPrefabs> {

    #region Prefabs

    /// <summary>
    /// A generic prefab for labels that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingLabel;
    /// <summary>
    /// A generic prefab for sprites that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingSprite;

    /// <summary>
    /// A specific prefab for labels that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingLabel uiTrackingLabel;
    /// <summary>
    /// A specific prefab for sprites that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingSprite uiTrackingSprite;

    public SphereCollider universeEdge;
    public Transform cameraDummyTarget;
    public SectorItem sector;

    public Orbiter orbiter;
    public MovingOrbiter movingOrbiter;
    public OrbiterForShips orbiterForShips;
    public MovingOrbiterForShips movingOrbiterForShips;

    public FleetCommandItem fleetCmd;
    public ShipItem[] ships;

    public SettlementCommandItem settlementCmd;

    public StarbaseCommandItem starbaseCmd;

    public FacilityItem[] facilities;

    public SystemItem system;   // without the star and settlement
    public StarItem[] stars;
    public PlanetItem[] planets;   // no bundled moons or orbiters
    public MoonItem[] moons;       // no orbiters

    public WeaponRangeMonitor weaponRangeMonitor;
    public SensorRangeMonitor sensorRangeMonitor;
    public FormationStationMonitor formationStation;

    #endregion

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


