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

    private const string PlanetNameFormat = "{0} {1}";
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
        D.Assert(systemParent != null);
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
        D.Assert(!star.IsOperational, "{0} should not be operational.", star.FullName);
        D.Assert(star.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(star.FullName));
        D.Assert(starStat.Category == star.category, "{0} {1} should = {2}.".Inject(typeof(StarCategory).Name, starStat.Category.GetValueName(), star.category.GetValueName()));

        star.Name = systemName + Constants.Space + CommonTerms.Star;
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
        D.Assert(orbitSlot.OrbitedItem != null, "{0}: {1}.OrbitedItem should not be null.", GetType().Name, typeof(OrbitData).Name);
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
        D.Assert(!planet.IsOperational, "{0} should not be operational.", planet.FullName);
        D.Assert(planet.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(planet.FullName));
        D.Assert(planetStat.Category == planet.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, planetStat.Category.GetValueName(), planet.category.GetValueName());
        D.Assert(orbitSlot.OrbitedItem != null, "{0}: {1}.OrbitedItem should not be null.", GetType().Name, typeof(OrbitData).Name);

        string parentSystemName = orbitSlot.OrbitedItem.name;
        planet.Name = PlanetNameFormat.Inject(parentSystemName, PlanetNumbers[orbitSlot.SlotIndex]);
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetData data = new PlanetData(planet, passiveCMs, planetStat) {
            ParentName = parentSystemName
        };
        planet.GetComponent<Rigidbody>().mass = data.Mass;  // 7.26.16 Not really needed as Planetoid Rigidbodies are kinematic
        planet.CameraStat = cameraStat;
        planet.Data = data;

        InstallCelestialItemInOrbit(planet.gameObject, orbitSlot);
    }


    /// <summary>
    /// Makes and returns a planet instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentSystem">The parent system.</param>
    /// <returns></returns>
    [Obsolete]
    public PlanetItem MakeInstance(string designName, FollowableItemCameraStat cameraStat, SystemItem parentSystem) {
        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
        return MakeInstance(design, cameraStat, parentSystem);
    }

    /// <summary>
    /// Makes and returns a planet instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentSystem">The parent system.</param>
    /// <returns></returns>
    [Obsolete]
    public PlanetItem MakeInstance(PlanetDesign design, FollowableItemCameraStat cameraStat, SystemItem parentSystem) {
        return MakeInstance(design.Stat, cameraStat, design.PassiveCmStats, parentSystem);
    }

    /// <summary>
    /// Makes an instance of a Planet based on the stat provided. The returned
    /// Item (with its Data)  will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentSystem">The parent system.</param>
    /// <returns></returns>
    [Obsolete]
    public PlanetItem MakeInstance(PlanetStat planetStat, FollowableItemCameraStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, SystemItem parentSystem) {
        GameObject planetPrefab = _planetPrefabs.Single(p => p.category == planetStat.Category).gameObject;
        GameObject planetGo = UnityUtility.AddChild(parentSystem.gameObject, planetPrefab);

        var planetItem = planetGo.GetSafeComponent<PlanetItem>();
        PopulateInstance(planetStat, cameraStat, cmStats, parentSystem.Data.Name, ref planetItem);
        return planetItem;
    }

    /// <summary>
    /// Populates the provided planet instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="planet">The planet.</param>
    [Obsolete]
    public void PopulateInstance(string designName, FollowableItemCameraStat cameraStat, string systemName, ref PlanetItem planet) {
        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
        PopulateInstance(design, cameraStat, systemName, ref planet);
    }

    /// <summary>
    /// Populates the provided planet instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="planet">The planet.</param>
    [Obsolete]
    public void PopulateInstance(PlanetDesign design, FollowableItemCameraStat cameraStat, string systemName, ref PlanetItem planet) {
        PopulateInstance(design.Stat, cameraStat, design.PassiveCmStats, systemName, ref planet);
    }

    /// <summary>
    /// Populates the item instance provided from the stats provided.
    /// The Item (with its Data)  will not be enabled. The item's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentSystemName">Name of the parent system.</param>
    /// <param name="planet">The planet item.</param>
    [Obsolete]
    public void PopulateInstance(PlanetStat planetStat, FollowableItemCameraStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats,
        string parentSystemName, ref PlanetItem planet) {
        D.Assert(!planet.IsOperational, "{0} should not be operational.", planet.FullName);
        D.Assert(planet.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(planet.FullName));
        D.Assert(planetStat.Category == planet.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, planetStat.Category.GetValueName(), planet.category.GetValueName());

        planet.Name = planetStat.Category.GetValueName();  // avoids Assert(Name != null), name gets updated when assigned to an orbit slot
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetData data = new PlanetData(planet, passiveCMs, planetStat) {
            ParentName = parentSystemName
        };
        planet.GetComponent<Rigidbody>().mass = data.Mass;  // 7.26.16 Not really needed as Planetoid Rigidbodies are kinematic
        planet.CameraStat = cameraStat;
        planet.Data = data;
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
        D.Assert(orbitSlot.OrbitedItem != null, "{0}: {1}.OrbitedItem should not be null.", GetType().Name, typeof(OrbitData).Name);
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
        D.Assert(!moon.IsOperational, "{0} should not be operational.", moon.FullName);
        D.Assert(moon.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(moon.FullName));
        D.Assert(moonStat.Category == moon.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, moonStat.Category.GetValueName(), moon.category.GetValueName());
        D.Assert(orbitSlot.OrbitedItem != null, "{0}: {1}.OrbitedItem should not be null.", GetType().Name, typeof(OrbitData).Name);

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

    /// <summary>
    /// Makes and returns a moon instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentPlanet">The parent planet.</param>
    /// <returns></returns>
    [Obsolete]
    public MoonItem MakeInstance(string designName, FollowableItemCameraStat cameraStat, PlanetItem parentPlanet) {
        MoonDesign design = _gameMgr.CelestialDesigns.GetMoonDesign(designName);
        return MakeInstance(design, cameraStat, parentPlanet);
    }

    /// <summary>
    /// Makes and returns a moon instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentPlanet">The parent planet.</param>
    /// <returns></returns>
    [Obsolete]
    public MoonItem MakeInstance(MoonDesign design, FollowableItemCameraStat cameraStat, PlanetItem parentPlanet) {
        return MakeInstance(design.Stat, cameraStat, design.PassiveCmStats, parentPlanet);
    }

    /// <summary>
    /// Makes an instance of a Moon based on the stat provided. The returned
    /// Item (with its Data)  will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="moonStat">The moon stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentPlanet">The parent planet.</param>
    /// <returns></returns>
    [Obsolete]
    public MoonItem MakeInstance(PlanetoidStat moonStat, FollowableItemCameraStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, PlanetItem parentPlanet) {
        GameObject moonPrefab = _moonPrefabs.Single(m => m.category == moonStat.Category).gameObject;
        GameObject moonGo = UnityUtility.AddChild(parentPlanet.gameObject, moonPrefab);

        var moonItem = moonGo.GetSafeComponent<MoonItem>();
        PopulateInstance(moonStat, cameraStat, cmStats, parentPlanet.Data.Name, ref moonItem);
        return moonItem;
    }

    /// <summary>
    /// Populates the provided moon instance.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentPlanetName">Name of the parent planet.</param>
    /// <param name="moon">The moon.</param>
    [Obsolete]
    public void PopulateInstance(string designName, FollowableItemCameraStat cameraStat, string parentPlanetName, ref MoonItem moon) {
        MoonDesign design = _gameMgr.CelestialDesigns.GetMoonDesign(designName);
        PopulateInstance(design, cameraStat, parentPlanetName, ref moon);
    }

    /// <summary>
    /// Populates the provided moon instance.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="parentPlanetName">Name of the parent planet.</param>
    /// <param name="moon">The moon.</param>
    [Obsolete]
    public void PopulateInstance(MoonDesign design, FollowableItemCameraStat cameraStat, string parentPlanetName, ref MoonItem moon) {
        PopulateInstance(design.Stat, cameraStat, design.PassiveCmStats, parentPlanetName, ref moon);
    }

    /// <summary>
    /// Populates the item instance provided from the stats provided.
    /// The Item (with its Data)  will not be enabled. The item's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="moonStat">The moon stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentPlanetName">Name of the parent planet.</param>
    /// <param name="moon">The item.</param>
    [Obsolete]
    public void PopulateInstance(PlanetoidStat moonStat, FollowableItemCameraStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats,
        string parentPlanetName, ref MoonItem moon) {
        D.Assert(!moon.IsOperational, "{0} should not be operational.", moon.FullName);
        D.Assert(moon.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(moon.FullName));
        D.Assert(moonStat.Category == moon.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, moonStat.Category.GetValueName(), moon.category.GetValueName());

        moon.Name = moonStat.Category.GetValueName();  // avoids Assert(Name != null), name gets updated when assigned to an orbit slot
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetoidData data = new PlanetoidData(moon, passiveCMs, moonStat) {
            ParentName = parentPlanetName
        };
        moon.GetComponent<Rigidbody>().mass = data.Mass;    // 7.26.16 Not really needed as Planetoid Rigidbodies are kinematic
        moon.CameraStat = cameraStat;
        moon.Data = data;
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
        D.Assert(!system.IsOperational, "{0} should not be operational.", system.FullName);
        D.Assert(system.transform.parent != null, "{0} should already have a parent.", system.FullName);

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
        D.Assert(!creatorGo.isStatic, "{0}: {1} should not start static as it has yet to be positioned.", GetType().Name, typeof(SystemCreator).Name);
        creatorGo.transform.position = location;
        creatorGo.isStatic = true;
        return creatorGo.GetComponent<SystemCreator>();
    }

    #endregion

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

    /// <summary>
    /// Installs the provided orbitingObject into orbit around the OrbitedObject held by orbitData.
    /// </summary>
    /// <param name="orbitingGo">The orbiting GameObject.</param>
    /// <param name="orbitData">The orbit slot.</param>
    private void InstallCelestialItemInOrbit(GameObject orbitingGo, OrbitData orbitData) {
        GameObject orbitSimPrefab = orbitData.IsOrbitedItemMobile ? _mobileCelestialOrbitSimPrefab.gameObject : _immobileCelestialOrbitSimPrefab.gameObject;
        GameObject orbitSimGo = UnityUtility.AddChild(orbitData.OrbitedItem, orbitSimPrefab);
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


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}



