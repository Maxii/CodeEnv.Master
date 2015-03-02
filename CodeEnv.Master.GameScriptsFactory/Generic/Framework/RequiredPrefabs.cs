// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RequiredPrefabs.cs
// Persistent singleton container that holds prefab instances that can be used to instantiate a clone in a scene.
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
/// Persistent singleton container that holds prefab instances that can be used to instantiate a clone in a scene.
/// </summary>
public class RequiredPrefabs : AMonoSingleton<RequiredPrefabs> {

    #region Prefabs

    public GameObject explosion;

    public GameObject hitImpact;

    /// <summary>
    /// A generic prefab for labels that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingLabel;
    /// <summary>
    /// A specific prefab for labels that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingLabel uiTrackingLabel;
    /// <summary>
    /// Prefab for sprites that track the world object they are parented too.
    /// They need specific scripts and an atlas added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingSprite;

    /// <summary>
    /// Prefab for sprites that track world objects from the UI layer.
    /// Includes all scripts but needs an atlas added.
    /// </summary>
    public UITrackingSprite uiTrackingSprite;

    public UIAtlas fleetIconAtlas;
    public UIAtlas contextualAtlas;

    public SphereCollider universeEdge;
    public Transform cameraDummyTarget;
    public SectorItem sector;

    public Orbiter orbiter;
    public MovingOrbiter movingOrbiter;
    public OrbiterForShips orbiterForShips;
    public MovingOrbiterForShips movingOrbiterForShips;

    public FleetCmdItem fleetCmd;
    public ShipItem[] ships;

    public SettlementCmdItem settlementCmd;

    public StarbaseCmdItem starbaseCmd;

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


