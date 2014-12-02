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

    /// <summary>
    /// The prefab used to make stars. This top level gameobject's name is the same as the StarCategory
    /// of the star. StarItem and StarView are components of this gameobject. Children include
    /// 1) a billboard with lights and corona textures, 2) the star mesh and 3) a keepoutzone collider.
    /// </summary>
    private GameObject[] _starPrefabs;

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

    protected override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;
        _starPrefabs = reqdPrefabs.stars.Select(s => s.gameObject).ToArray();
        _planetPrefabs = reqdPrefabs.planets;
        _systemPrefab = reqdPrefabs.system;
        _moonPrefabs = reqdPrefabs.moons;
    }

    /// <summary>
    /// Makes an instance of a Star based on the stat provided. The returned Item (with its Data) 
    /// will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="systemParent">The system parent.</param>
    /// <param name="starLayer">The layer you want the star to assume. Default is Layers.Default.</param>
    /// <returns></returns>
    public StarItem MakeInstance(StarStat starStat, SystemItem systemParent, Layers starLayer = Layers.Default) {
        GameObject starPrefab = _starPrefabs.First(sGo => sGo.name == starStat.Category.GetName());
        GameObject starGo = UnityUtility.AddChild(systemParent.gameObject, starPrefab);
        starGo.layer = (int)starLayer;
        StarItem item = starGo.GetSafeMonoBehaviourComponent<StarItem>();
        MakeInstance(starStat, systemParent.Data.Name, ref item);
        return item;
    }

    /// <summary>
    /// Makes an instance of a Star from the stat and item provided. The Item (with its Data)  will not be enabled.
    /// The item's transform will have the same layer and same parent it arrived with.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="star">The star item.</param>
    public void MakeInstance(StarStat starStat, string systemName, ref StarItem star) {
        D.Assert(!star.enabled, "{0} should not be enabled.".Inject(star.FullName));
        D.Assert(star.transform.parent != null, "{0} should already have a parent.".Inject(star.FullName));
        D.Assert(starStat.Category == star.category, "{0} {1} should = {2}.".Inject(typeof(StarCategory).Name, starStat.Category.GetName(), star.category.GetName()));

        string starName = systemName + Constants.Space + CommonTerms.Star;
        star.Data = new StarData(starStat) {
            Name = starName,
            ParentName = systemName
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
        };
    }

    /// <summary>
    /// Makes an instance of a Planet based on the stat provided. The returned
    /// Item (with its Data)  will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentSystem">The parent system.</param>
    /// <returns></returns>
    public PlanetItem MakeInstance(PlanetoidStat planetStat, IEnumerable<CountermeasureStat> cmStats, SystemItem parentSystem) {
        GameObject planetPrefab = _planetPrefabs.Single(p => p.category == planetStat.Category).gameObject;
        GameObject planetGo = UnityUtility.AddChild(parentSystem.gameObject, planetPrefab);

        var planetItem = planetGo.GetSafeMonoBehaviourComponent<PlanetItem>();
        MakeInstance(planetStat, cmStats, parentSystem.Data.Name, ref planetItem);
        return planetItem;
    }

    /// <summary>
    /// Makes an instance of a Planet from the stat and item provided.
    /// The Item (with its Data)  will not be enabled. The item's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentSystemName">Name of the parent system.</param>
    /// <param name="planet">The planet item.</param>
    public void MakeInstance(PlanetoidStat planetStat, IEnumerable<CountermeasureStat> cmStats, string parentSystemName, ref PlanetItem planet) {
        D.Assert(!planet.enabled, "{0} should not be enabled.".Inject(planet.FullName));
        D.Assert(planet.transform.parent != null, "{0} should already have a parent.".Inject(planet.FullName));
        D.Assert(planetStat.Category == planet.category,
            "{0} {1} should = {2}.".Inject(typeof(PlanetoidCategory).Name, planetStat.Category.GetName(), planet.category.GetName()));

        planet.Data = new PlanetData(planetStat) {
            ParentName = parentSystemName
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
            // CombatStrength is default(CombatStrength), aka all values zero'd out
        };
        AttachCountermeasures(cmStats, planet);
    }

    /// <summary>
    /// Makes an instance of a Moon based on the stat provided. The returned
    /// Item (with its Data)  will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="moonStat">The moon stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentPlanet">The parent planet.</param>
    /// <returns></returns>
    public MoonItem MakeInstance(PlanetoidStat moonStat, IEnumerable<CountermeasureStat> cmStats, PlanetItem parentPlanet) {
        GameObject moonPrefab = _moonPrefabs.Single(m => m.category == moonStat.Category).gameObject;
        GameObject moonGo = UnityUtility.AddChild(parentPlanet.gameObject, moonPrefab);

        var moonItem = moonGo.GetSafeMonoBehaviourComponent<MoonItem>();
        MakeInstance(moonStat, cmStats, parentPlanet.Data.Name, ref moonItem);
        return moonItem;
    }

    /// <summary>
    /// Makes an instance of a Moon from the stat and item provided.
    /// The Item (with its Data)  will not be enabled. The item's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="moonStat">The planet stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="parentPlanetName">Name of the parent planet.</param>
    /// <param name="moon">The item.</param>
    public void MakeInstance(PlanetoidStat moonStat, IEnumerable<CountermeasureStat> cmStats, string parentPlanetName, ref MoonItem moon) {
        D.Assert(!moon.enabled, "{0} should not be enabled.".Inject(moon.FullName));
        D.Assert(moon.transform.parent != null, "{0} should already have a parent.".Inject(moon.FullName));
        D.Assert(moonStat.Category == moon.category,
            "{0} {1} should = {2}.".Inject(typeof(PlanetoidCategory).Name, moonStat.Category.GetName(), moon.category.GetName()));

        moon.Data = new MoonData(moonStat) {
            ParentName = parentPlanetName
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
            // CombatStrength is default(CombatStrength), aka all values zero'd out
        };
        AttachCountermeasures(cmStats, moon);
    }

    /// <summary>
    /// Makes an instance of a System from the name provided. The returned Item (with its Data) 
    /// will not be enabled but their gameObject will be parented to the provided parent. Their are
    /// no subordinate planets or stars attached yet.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="creatorParent">The creator parent.</param>
    /// <returns></returns>
    public SystemItem MakeSystemInstance(Index3D sectorIndex, Topography topography, SystemCreator creatorParent) {
        GameObject systemPrefab = _systemPrefab.gameObject;
        GameObject systemGo = UnityUtility.AddChild(creatorParent.gameObject, systemPrefab);
        string systemName = creatorParent.SystemName;
        systemGo.name = systemName;
        SystemItem item = systemGo.GetSafeMonoBehaviourComponent<SystemItem>();
        MakeSystemInstance(systemName, sectorIndex, topography, ref item);
        return item;
    }

    /// <summary>
    /// Makes an instance of a System from the name and item provided. The Item (with its Data) 
    /// will not be enabled. The item's transform will have the same parent and children it arrived with.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="system">The system item.</param>
    public void MakeSystemInstance(string systemName, Index3D sectorIndex, Topography topography, ref SystemItem system) {
        D.Assert(system.transform.parent != null, "{0} should already have a parent.".Inject(system.FullName));
        SystemData data = new SystemData(systemName, sectorIndex, topography) {
            // Owners are all initialized to TempGameValues.NoPlayer by AItemData
        };
        system.Data = data;
    }

    private void AttachCountermeasures(IEnumerable<CountermeasureStat> cmStats, AMortalItem mortalItem) {
        cmStats.ForAll(cmStat => mortalItem.AddCountermeasure(cmStat));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}



