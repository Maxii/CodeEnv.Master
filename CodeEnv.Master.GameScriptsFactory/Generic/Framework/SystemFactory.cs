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

    /// <summary>
    /// The prefab used to make stars. This top level gameobject's name is the same as the StarCategory
    /// of the star. StarItem and StarView are components of this gameobject. Children include
    /// 1) a billboard with lights and corona textures, 2) the star mesh and 3) a keepoutzone collider.
    /// </summary>
    private StarItem[] _starPrefabs;

    /// <summary>
    /// The prefab used to make a planet. Children of the planet include 1) the atmosphere mesh, 2) the planet mesh 
    /// and 3) a keepoutzone collider.
    /// Note: previous versions contained one or more moons and the orbiters to go with them. Containing all of
    /// this was a top level empty gameobject whose name was the same as the PlanetoidCategory of the planet. 
    /// </summary>
    private PlanetItem[] _planetPrefabs;

    /// <summary>
    /// The prefab used to make a system. This top level gameobject's components include the System Item and
    /// VIew. It's child is an empty orbital plane object whose children include a plane mesh and highlight mesh. There 
    /// are other children and grandchildren but they are only transitional.
    /// </summary>
    private SystemItem _systemPrefab;

    /// <summary>
    /// The prefab used to make a moon. Children of the moon include 1)  the planet mesh  and 2) a keepoutzone collider.
    /// Note: previously all moons were embedded in the planet prefab.
    /// </summary>
    private MoonItem[] _moonPrefabs;

    private SystemFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;
        _starPrefabs = reqdPrefabs.stars;
        _planetPrefabs = reqdPrefabs.planets;
        _systemPrefab = reqdPrefabs.system;
        _moonPrefabs = reqdPrefabs.moons;
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
    public StarItem MakeInstance(StarStat starStat, CameraFocusableStat cameraStat, GameObject systemParent, string systemName) {
        D.Assert(systemParent != null);
        StarItem starPrefab = _starPrefabs.Single(star => star.category == starStat.Category);
        GameObject starGo = UnityUtility.AddChild(systemParent, starPrefab.gameObject);
        starGo.layer = (int)Layers.Default;
        StarItem starItem = starGo.GetSafeComponent<StarItem>();
        PopulateInstance(starStat, cameraStat, systemName, ref starItem);
        return starItem;
    }

    /// <summary>
    /// Populates the item instance provided from the stats provided. The Item (with its Data)  will not be enabled.
    /// The item's transform will have the same layer and same parent it arrived with.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="star">The star item.</param>
    public void PopulateInstance(StarStat starStat, CameraFocusableStat cameraStat, string systemName, ref StarItem star) {
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

    /// <summary>
    /// Makes an instance of a Planet based on the stat provided. The returned
    /// Item (with its Data)  will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentSystem">The parent system.</param>
    /// <returns></returns>
    public PlanetItem MakeInstance(PlanetStat planetStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, SystemItem parentSystem) {
        GameObject planetPrefab = _planetPrefabs.Single(p => p.category == planetStat.Category).gameObject;
        GameObject planetGo = UnityUtility.AddChild(parentSystem.gameObject, planetPrefab);

        var planetItem = planetGo.GetSafeComponent<PlanetItem>();
        PopulateInstance(planetStat, cameraStat, cmStats, parentSystem.Data.Name, ref planetItem);
        return planetItem;
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
    public void PopulateInstance(PlanetStat planetStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, string parentSystemName, ref PlanetItem planet) {
        D.Assert(!planet.IsOperational, "{0} should not be operational.", planet.FullName);
        D.Assert(planet.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(planet.FullName));
        D.Assert(planetStat.Category == planet.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, planetStat.Category.GetValueName(), planet.category.GetValueName());

        planet.Name = planetStat.Category.GetValueName();  // avoids Assert(Name != null), name gets updated when assigned to an orbit slot
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetData data = new PlanetData(planet, passiveCMs, planetStat) {
            ParentName = parentSystemName
        };
        planet.GetComponent<Rigidbody>().mass = data.Mass;
        planet.CameraStat = cameraStat;
        planet.Data = data;
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
    public MoonItem MakeInstance(PlanetoidStat moonStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, PlanetItem parentPlanet) {
        GameObject moonPrefab = _moonPrefabs.Single(m => m.category == moonStat.Category).gameObject;
        GameObject moonGo = UnityUtility.AddChild(parentPlanet.gameObject, moonPrefab);

        var moonItem = moonGo.GetSafeComponent<MoonItem>();
        PopulateInstance(moonStat, cameraStat, cmStats, parentPlanet.Data.Name, ref moonItem);
        return moonItem;
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
    public void PopulateInstance(PlanetoidStat moonStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, string parentPlanetName, ref MoonItem moon) {
        D.Assert(!moon.IsOperational, "{0} should not be operational.", moon.FullName);
        D.Assert(moon.GetComponentInParent<SystemItem>() != null, "{0} must have a system parent before data assigned.".Inject(moon.FullName));
        D.Assert(moonStat.Category == moon.category,
            "{0} {1} should = {2}.", typeof(PlanetoidCategory).Name, moonStat.Category.GetValueName(), moon.category.GetValueName());

        moon.Name = moonStat.Category.GetValueName();  // avoids Assert(Name != null), name gets updated when assigned to an orbit slot
        var passiveCMs = MakeCountermeasures(cmStats);
        PlanetoidData data = new PlanetoidData(moon, passiveCMs, moonStat) {
            ParentName = parentPlanetName
        };
        moon.GetComponent<Rigidbody>().mass = data.Mass;
        moon.CameraStat = cameraStat;
        moon.Data = data;
    }

    /// <summary>
    /// Makes an instance of a System from the name provided. The returned Item (with its Data)
    /// will not be enabled but their gameObject will be parented to the provided parent. Their are
    /// no subordinate planets or stars attached yet.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="parent">The GameObject the System should be a child of.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <returns></returns>
    public SystemItem MakeSystemInstance(string systemName, GameObject parent, CameraFocusableStat cameraStat) {
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
    public void PopulateSystemInstance(string systemName, CameraFocusableStat cameraStat, ref SystemItem system) {
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
    /// Makes and returns passive countermeasures made from the provided stats. PassiveCountermeasures donot use RangeMonitors.
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



