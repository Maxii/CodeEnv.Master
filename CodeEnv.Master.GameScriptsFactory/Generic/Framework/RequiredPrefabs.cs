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
/// WARNING: These prefab references must be instantiated in the scene to use them.
/// </summary>
public class RequiredPrefabs : AMonoSingleton<RequiredPrefabs> {

    #region Prefabs

    [Header("Tracking Widgets")]
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

    [Header("Sprite Atlases")]
    public UIAtlas fleetIconAtlas;
    public UIAtlas contextualAtlas;
    public UIAtlas myGuiAtlas;

    [Header("Misc")]
    public SectorItem sector;
    public FleetFormationStation fleetFormationStation;

    [Header("Orbit Simulators")]
    public OrbitSimulator orbitSimulator;
    public MobileOrbitSimulator mobileOrbitSimulator;
    public ShipCloseOrbitSimulator shipCloseOrbitSimulator;
    public MobileShipCloseOrbitSimulator mobileShipCloseOrbitSimulator;

    [Header("Commands")]
    public FleetCmdItem fleetCmd;
    public SettlementCmdItem settlementCmd;
    public StarbaseCmdItem starbaseCmd;

    [Header("Ships")]
    public ShipItem shipItem;
    public ShipHull[] shipHulls;

    [Header("Facilities")]
    public FacilityItem facilityItem;
    public FacilityHull[] facilityHulls;

    [Header("WeaponMounts")]
    public MissileTube missileTube; // Up
    public LOSTurret losTurret; // Up

    [Header("System")]
    public SystemItem system;   // without the star and settlement

    [Header("Stars")]
    public StarItem[] stars;

    [Header("Planets")]
    public PlanetItem[] planets;   // no bundled moons or orbiters

    [Header("Moons")]
    public MoonItem[] moons;       // no orbiters

    [Header("Collider Monitors")]
    public ActiveCountermeasureRangeMonitor countermeasureRangeMonitor;
    public Shield shield;
    public WeaponRangeMonitor weaponRangeMonitor;
    public SensorRangeMonitor sensorRangeMonitor;

    #endregion

    public override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


