// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemFactory.cs
// Singleton factory that makes instances of Stars, Planets, Moons and Systems.
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
/// Singleton factory that makes instances of Stars, Planets, Moons and Systems.
/// 
/// Note on ownership: Systems, Stars, Planets and Moons all have the same owner, the owner of the Settlement,
/// if any, that is present in the System. The owner value held in each data is automatically changed by
/// SystemData when a Settlement owner changes, or the settlement is added or removed from the system.
/// </summary>
public class SystemFactory : AGenericSingleton<SystemFactory> {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private const string StarNameFormat = "{0} {1}";
    private const string PlanetNameFormat = "{0}{1}";
    private const string MoonNameFormat = "{0}{1}";

    private static int[] PlanetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static string[] MoonLetters = new string[] { "a", "b", "c", "d", "e" };


    /// <summary>
    /// The prefab used to make stars. Children include
    /// a billboard with lights and corona textures, the star mesh and a keep out zone collider.
    /// </summary>
    private StarItem[] _starPrefabs;

    /// <summary>
    /// The prefab used to make a planet. Children of the planet include the atmosphere mesh, the planet mesh 
    /// and a keep out zone collider.
    /// Note: previous versions contained one or more moons and the orbiters to go with them. Containing all of
    /// this was a top level empty gameObject whose name was the same as the PlanetoidCategory of the planet. 
    /// </summary>
    private PlanetItem[] _planetPrefabs;

    /// <summary>
    /// The prefab used to make a system. The children of this SystemItem include an orbital plane object, 
    /// topography monitor and folder for holding the system's planets.
    /// </summary>
    private SystemItem _systemPrefab;

    /// <summary>
    /// The prefab used to make a moon. Children of the moon include the moon mesh and a keep out zone collider.
    /// Note: previously all moons were embedded in the planet prefab.
    /// </summary>
    private MoonItem[] _moonPrefabs;

    /// <summary>
    /// The prefab used to make a SystemCreator. There are no children of a SystemCreator.
    /// </summary>
    private SystemCreator _systemCreatorPrefab;
    private OrbitSimulator _immobileCelestialOrbitSimPrefab;
    private MobileOrbitSimulator _mobileCelestialOrbitSimPrefab;

    private GameManager _gameMgr;

    private SystemFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        _gameMgr = GameManager.Instance;
        var reqdPrefabs = RequiredPrefabs.Instance;
        _starPrefabs = reqdPrefabs.stars;
        _planetPrefabs = reqdPrefabs.planets;
        _systemPrefab = reqdPrefabs.system;
        _moonPrefabs = reqdPrefabs.moons;
        _systemCreatorPrefab = reqdPrefabs.systemCreator;
        _immobileCelestialOrbitSimPrefab = reqdPrefabs.orbitSimulator;
        _mobileCelestialOrbitSimPrefab = reqdPrefabs.mobileOrbitSimulator;
    }

    #region Stars

    /// <summary>
    /// Makes and returns a star instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemParent">The system parent.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <returns></returns>
    public StarItem MakeInstance(string designName, FocusableItemCameraStat cameraStat, GameObject systemParent, string systemName) {
        StarDesign design = _gameMgr.CelestialDesigns.GetStarDesign(designName);
        return MakeInstance(design, cameraStat, systemParent, systemName);
    }

    /// <summary>
    /// Makes and returns a star instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemParent">The system parent.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <returns></returns>
    public StarItem MakeInstance(StarDesign design, FocusableItemCameraStat cameraStat, GameObject systemParent, string systemName) {
        return MakeInstance(design.StarStat, cameraStat, systemParent, systemName);
    }

    /// <summary>
    /// Makes an instance of a Star based on the stat provided. The returned Item (with its Data)
    /// will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemParent">The system parent of the star.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <returns></returns>
    public StarItem MakeInstance(StarStat starStat, FocusableItemCameraStat cameraStat, GameObject systemParent, string systemName) {
        D.AssertNotNull(systemParent);
        StarItem starPrefab = _starPrefabs.Single(star => star.category == starStat.Category);
        GameObject starGo = UnityUtility.AddChild(systemParent, starPrefab.gameObject);
        starGo.layer = (int)Layers.Default;
        StarItem starItem = starGo.GetSafeComponent<StarItem>();
        PopulateInstance(starStat, cameraStat, systemName, ref starItem);
        return starItem;
    }

    /// <summary>
    /// Populates the provided star instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="star">The star.</param>
    public void PopulateInstance(string designName, FocusableItemCameraStat cameraStat, string systemName, ref StarItem star) {
        StarDesign design = _gameMgr.CelestialDesigns.GetStarDesign(designName);
        PopulateInstance(design, cameraStat, systemName, ref star);
    }

    /// <summary>
    /// Populates the provided star instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="star">The star.</param>
    public void PopulateInstance(StarDesign design, FocusableItemCameraStat cameraStat, string systemName, ref StarItem star) {
        PopulateInstance(design.StarStat, cameraStat, systemName, ref star);
    }

    /// <summary>
    /// Populates the item instance provided from the stats provided. The Item (with its Data)  will not be enabled.
    /// The item's transform will have the same layer and same parent it arrived with.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="star">The star item.</param>
    public void PopulateInstance(StarStat starStat, FocusableItemCameraStat cameraStat, string systemName, ref StarItem star) {
        D.Assert(!star.IsOperational, star.FullName);
        if (star.GetComponentInParent<SystemItem>() == null) {
            D.Error("{0}: {1} must have a system parent before data assigned.", Name, star.FullName);
        }
        if (starStat.Category != star.category) {
            D.Error("{0}: {1} should = {2}.", Name, starStat.Category.GetValueName(), star.category.GetValueName());
        }

        star.Name = StarNameFormat.Inject(systemName, CommonTerms.Star);
        StarData starData = new StarData(star, starStat) {
            ParentName = systemName
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
        };
        star.CameraStat = cameraStat;
        star.Data = starData;
    }

    #endregion

    #region Planets

    /// <summary>
    /// Makes and returns a planet instance that has been placed in orbit around the System specified by <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <returns></returns>
    public PlanetItem MakePlanetInstance(string designName, FollowableItemCameraStat cameraStat, OrbitData orbitSlot) {
        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
        return MakeInstance(design, cameraStat, orbitSlot);
    }

    /// <summary>
    /// Makes and returns a planet instance that has been placed in orbit around the System specified by <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <returns></returns>
    public PlanetItem MakeInstance(PlanetDesign design, FollowableItemCameraStat cameraStat, OrbitData orbitSlot) {
        return MakeInstance(design.Stat, cameraStat, orbitSlot, design.PassiveCmStats);
    }

    /// <summary>
    /// Makes and returns a planet instance that has been placed in orbit around the System specified by <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <returns></returns>
    public PlanetItem MakeInstance(PlanetStat planetStat, FollowableItemCameraStat cameraStat, OrbitData orbitSlot,
        IEnumerable<PassiveCountermeasureStat> cmStats) {
        D.AssertNotNull(orbitSlot.OrbitedItem, orbitSlot.ToString());
        GameObject planetPrefab = _planetPrefabs.Single(p => p.category == planetStat.Category).gameObject;
        GameObject planetGo = UnityUtility.AddChild(orbitSlot.OrbitedItem, planetPrefab);

        var planetItem = planetGo.GetSafeComponent<PlanetItem>();
        PopulateInstance(planetStat, cameraStat, orbitSlot, cmStats, ref planetItem);
        return planetItem;
    }

    /// <summary>
    /// Populates the provided <c>planet</c> with data and places it in orbit around the System specified in <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="planet">The planet.</param>
    public void PopulateInstance(string designName, FollowableItemCameraStat cameraStat, OrbitData orbitSlot, ref PlanetItem planet) {
        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
        PopulateInstance(design, cameraStat, orbitSlot, ref planet);
    }

    /// <summary>
    /// Populates the provided <c>planet</c> with data and places it in orbit around the System specified in <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="planet">The planet.</param>
    public void PopulateInstance(PlanetDesign design, FollowableItemCameraStat cameraStat, OrbitData orbitSlot, ref PlanetItem planet) {
        PopulateInstance(design.Stat, cameraStat, orbitSlot, design.PassiveCmStats, ref planet);
    }

    /// <summary>
    /// Populates the provided <c>planet</c> with data and places it in orbit around the System specified in <c>orbitSlot</c>.
    /// The planet will not be enabled.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="planet">The planet item.</param>
    public void PopulateInstance(PlanetStat planetStat, FollowableItemCameraStat cameraStat, OrbitData orbitSlot, IEnumerable<PassiveCountermeasureStat> cmStats,
        ref PlanetItem planet) {
        D.Assert(!planet.IsOperational, planet.FullName);
        if (planet.GetComponentInParent<SystemItem>() == null) {
            D.Error("{0}: {1} must have a system parent before data assigned.", Name, planet.FullName);
        }
        if (planetStat.Category != planet.category) {
            D.Error("{0}: {1} {2} should = {3}.", Name, typeof(PlanetoidCategory).Name, planetStat.Category.GetValueName(), planet.category.GetValueName());
        }
        D.AssertNotNull(orbitSlot.OrbitedItem, orbitSlot.ToString());

        string systemName = orbitSlot.OrbitedItem.name;
        planet.Name = PlanetNameFormat.Inject(systemName, PlanetNumbers[orbitSlot.SlotIndex]);
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetData data = new PlanetData(planet, passiveCMs, planetStat) {
            ParentName = systemName
        };
        planet.GetComponent<Rigidbody>().mass = data.Mass;  // 7.26.16 Not really needed as Planetoid Rigidbodies are kinematic
        planet.CameraStat = cameraStat;
        planet.Data = data;

        GameObject planetsFolder = orbitSlot.OrbitedItem.GetComponentsInImmediateChildren<Transform>().Single(t => t.name == "Planets").gameObject;
        InstallCelestialItemInOrbit(planet.gameObject, orbitSlot, planetsFolder);
    }

    #endregion

    #region Moons

    /// <summary>
    /// Makes and returns a moon instance that has been placed in orbit around the planet specified by <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <returns></returns>
    public MoonItem MakeMoonInstance(string designName, FollowableItemCameraStat cameraStat, OrbitData orbitSlot) {
        MoonDesign design = _gameMgr.CelestialDesigns.GetMoonDesign(designName);
        return MakeInstance(design, cameraStat, orbitSlot);
    }

    /// <summary>
    /// Makes and returns a moon instance that has been placed in orbit around the planet specified by <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <returns></returns>
    public MoonItem MakeInstance(MoonDesign design, FollowableItemCameraStat cameraStat, OrbitData orbitSlot) {
        return MakeInstance(design.Stat, cameraStat, orbitSlot, design.PassiveCmStats);
    }

    /// <summary>
    /// Makes and returns a moon instance that has been placed in orbit around the planet specified by <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="moonStat">The moon stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <returns></returns>
    public MoonItem MakeInstance(PlanetoidStat moonStat, FollowableItemCameraStat cameraStat, OrbitData orbitSlot,
        IEnumerable<PassiveCountermeasureStat> cmStats) {
        D.AssertNotNull(orbitSlot.OrbitedItem, orbitSlot.ToString());
        GameObject moonPrefab = _moonPrefabs.Single(m => m.category == moonStat.Category).gameObject;
        GameObject moonGo = UnityUtility.AddChild(orbitSlot.OrbitedItem, moonPrefab);

        var moonItem = moonGo.GetSafeComponent<MoonItem>();
        PopulateInstance(moonStat, cameraStat, orbitSlot, cmStats, ref moonItem);
        return moonItem;
    }

    /// <summary>
    /// Populates the provided <c>moon</c> with data and places it in orbit around the planet specified in <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="moon">The moon.</param>
    public void PopulateInstance(string designName, FollowableItemCameraStat cameraStat, OrbitData orbitSlot, ref MoonItem moon) {
        MoonDesign design = _gameMgr.CelestialDesigns.GetMoonDesign(designName);
        PopulateInstance(design, cameraStat, orbitSlot, ref moon);
    }

    /// <summary>
    /// Populates the provided <c>moon</c> with data and places it in orbit around the planet specified in <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="moon">The moon.</param>
    public void PopulateInstance(MoonDesign design, FollowableItemCameraStat cameraStat, OrbitData orbitSlot, ref MoonItem moon) {
        PopulateInstance(design.Stat, cameraStat, orbitSlot, design.PassiveCmStats, ref moon);
    }

    /// <summary>
    /// Populates the provided <c>moon</c> with data and places it in orbit around the planet specified in <c>orbitSlot</c>.
    /// The moon will not be enabled.
    /// </summary>
    /// <param name="moonStat">The moon stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="orbitSlot">The orbit slot.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="moon">The item.</param>
    public void PopulateInstance(PlanetoidStat moonStat, FollowableItemCameraStat cameraStat, OrbitData orbitSlot,
        IEnumerable<PassiveCountermeasureStat> cmStats, ref MoonItem moon) {
        D.Assert(!moon.IsOperational, moon.FullName);
        if (moon.GetComponentInParent<SystemItem>() == null) {
            D.Error("{0}: {1} must have a system parent before data assigned.", Name, moon.FullName);
        }
        if (moonStat.Category != moon.category) {
            D.Error("{0}: {1} {2} should = {3}.", Name, typeof(PlanetoidCategory).Name, moonStat.Category.GetValueName(), moon.category.GetValueName());
        }
        D.AssertNotNull(orbitSlot.OrbitedItem, orbitSlot.ToString());

        string parentPlanetName = orbitSlot.OrbitedItem.name;
        moon.Name = MoonNameFormat.Inject(parentPlanetName, MoonLetters[orbitSlot.SlotIndex]);

        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetoidData data = new PlanetoidData(moon, passiveCMs, moonStat) {
            ParentName = parentPlanetName
        };
        moon.GetComponent<Rigidbody>().mass = data.Mass;    // 7.26.16 Not really needed as Planetoid Rigidbodies are kinematic
        moon.CameraStat = cameraStat;
        moon.Data = data;

        InstallCelestialItemInOrbit(moon.gameObject, orbitSlot);
    }

    #endregion

    #region Systems

    /// <summary>
    /// Makes an instance of a System from the name provided. The returned Item (with its Data)
    /// will not be enabled but their gameObject will be parented to the provided parent. There are
    /// no subordinate planets or stars attached yet.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="parent">The GameObject the System should be a child of.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <returns></returns>
    public SystemItem MakeSystemInstance(string systemName, GameObject parent, FocusableItemCameraStat cameraStat) {
        GameObject systemPrefab = _systemPrefab.gameObject;
        GameObject systemGo = UnityUtility.AddChild(parent, systemPrefab);
        systemGo.name = systemName;
        SystemItem item = systemGo.GetSafeComponent<SystemItem>();
        PopulateSystemInstance(systemName, cameraStat, ref item);
        return item;
    }

    /// <summary>
    /// Populates the item instance provided from the stat provided. The Item (with its Data) 
    /// will not be enabled. The item's transform will have the same parent and children it arrived with.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="system">The system item.</param>
    public void PopulateSystemInstance(string systemName, FocusableItemCameraStat cameraStat, ref SystemItem system) {
        D.Assert(!system.IsOperational, system.FullName);
        D.AssertNotNull(system.transform.parent, system.FullName);

        system.Name = systemName;
        SystemData data = new SystemData(system) {
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
        };
        system.CameraStat = cameraStat;
        system.Data = data;
    }

    /// <summary>
    /// Makes an unnamed SystemCreator instance at the provided location, parented to the SystemsFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <returns></returns>
    public SystemCreator MakeCreatorInstance(Vector3 location) {
        GameObject creatorPrefabGo = _systemCreatorPrefab.gameObject;
        GameObject creatorGo = UnityUtility.AddChild(SystemsFolder.Instance.gameObject, creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", Name, typeof(SystemCreator).Name);
        }
        creatorGo.transform.position = location;
        creatorGo.isStatic = true;
        return creatorGo.GetSafeComponent<SystemCreator>();
    }

    #endregion

    /// <summary>
    /// Installs the provided orbitingObject into orbit around the OrbitedObject held by orbitData.
    /// If altParent is not set, the orbitingObject's parent OrbitSimulator becomes a child of OrbitedObject.
    /// If altParent is set, the parent OrbitSimulator becomes a child of altParent.
    /// <remarks>altParent is principally used to place orbit simulators under a altParent folder, aka a 
    /// System's planets are organized under the System's Planets folder.</remarks>
    /// </summary>
    /// <param name="orbitingGo">The orbiting GameObject.</param>
    /// <param name="orbitData">The orbit slot.</param>
    /// <param name="altParent">The alternative parent for the orbitingGo's parent OrbitSimulator.</param>
    public void InstallCelestialItemInOrbit(GameObject orbitingGo, OrbitData orbitData, GameObject altParent = null) {
        GameObject orbitSimPrefab = orbitData.IsOrbitedItemMobile ? _mobileCelestialOrbitSimPrefab.gameObject : _immobileCelestialOrbitSimPrefab.gameObject;
        GameObject orbitSimParent = altParent == null ? orbitData.OrbitedItem : altParent;
        GameObject orbitSimGo = UnityUtility.AddChild(orbitSimParent, orbitSimPrefab);
        var orbitSim = orbitSimGo.GetSafeComponent<OrbitSimulator>();
        orbitSim.OrbitData = orbitData;
        orbitSimGo.name = orbitingGo.name + Constants.Space + typeof(OrbitSimulator).Name;
        UnityUtility.AttachChildToParent(orbitingGo, orbitSimGo);
        orbitingGo.transform.localPosition = GenerateRandomLocalPositionWithinSlot(orbitData);
    }

    /// <summary>
    /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
    /// Use to set the local position of the orbiting object once attached to the orbiter.
    /// </summary>
    /// <returns></returns>
    private Vector3 GenerateRandomLocalPositionWithinSlot(OrbitData orbitData) {
        Vector2 pointOnCircle = RandomExtended.PointOnCircle(orbitData.MeanRadius);
        return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
    }

    /// <summary>
    /// Makes and returns passive countermeasures made from the provided stats. PassiveCountermeasures do not use RangeMonitors.
    /// </summary>
    /// <param name="cmStats">The cm stats.</param>
    /// <returns></returns>
    private IEnumerable<PassiveCountermeasure> MakeCountermeasures(IEnumerable<PassiveCountermeasureStat> cmStats) {
        var passiveCMs = new List<PassiveCountermeasure>(cmStats.Count());
        cmStats.ForAll(stat => passiveCMs.Add(new PassiveCountermeasure(stat)));
        return passiveCMs;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}



