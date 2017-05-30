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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

    [Header("UI Tracking Widgets")]
    /// <summary>
    /// A specific prefab for labels that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingLabel uiTrackingLabel;

    /// <summary>
    /// Prefab for sprites that track world objects from the UI layer.
    /// Includes all scripts but needs an atlas added.
    /// </summary>
    public UITrackingSprite uiTrackingSprite;

    [Header("World Tracking Widgets")]
    /// <summary>
    /// Prefab for labels that track a world object. The label is parented to a common
    /// UIPanel. They need specific scripts and an atlas added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingLabel;

    /// <summary>
    /// Prefab for sprites that track a world object. The sprite is parented to a common
    /// UIPanel. They need specific scripts and an atlas added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingSprite;

    [Header("Independent World Tracking Widgets")]
    /// <summary>
    /// A generic prefab for labels that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingLabel_Independent;

    /// <summary>
    /// Prefab for sprites that track the world object they are parented too.
    /// They need specific scripts and an atlas added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingSprite_Independent;


    [Header("Sprite Atlases")]
    public UIAtlas fleetIconAtlas;
    public UIAtlas contextualAtlas;
    public UIAtlas myGuiAtlas;

    [Header("Formations")]
    public GameObject globeFormation;
    public GameObject wedgeFormation;
    public GameObject planeFormation;
    public GameObject diamondFormation;
    public GameObject spreadFormation;

    [Header("Orbit Simulators")]
    public OrbitSimulator orbitSimulator;
    public MobileOrbitSimulator mobileOrbitSimulator;
    public ShipCloseOrbitSimulator shipCloseOrbitSimulator;
    public MobileShipCloseOrbitSimulator mobileShipCloseOrbitSimulator;
    public Rigidbody highOrbitAttachPoint;

    [Header("Creators")]
    public FleetCreator fleetCreator;
    public StarbaseCreator starbaseCreator;
    public SettlementCreator settlementCreator;
    public SystemCreator systemCreator;

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
    public LaunchTube launchTube; // Up
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
    public CmdSensorRangeMonitor cmdSensorRangeMonitor;
    public ElementSensorRangeMonitor elementSensorRangeMonitor;
    public FtlDampenerRangeMonitor ftlDampenerRangeMonitor;

    #endregion

    public string DebugName { get { return GetType().Name; } }

    public override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}


