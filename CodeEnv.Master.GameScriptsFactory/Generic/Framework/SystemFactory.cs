// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemFactory.cs
// Singleton factory that makes instances of Stars, Planets (with attached moons) and Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton factory that makes instances of Stars, Planets (with attached moons) and Systems.
/// </summary>
public class SystemFactory : AGenericSingleton<SystemFactory> {

    /// <summary>
    /// The prefab used to make stars. This top level gameobject's name is the same as the StarCategory
    /// of the star. StarModel and StarView are components of this gameobject. Children include
    /// 1) a billboard with lights and corona texture children, 2) the star mesh and 3) a keepoutzone collider.
    /// </summary>
    private GameObject[] _starPrefabs;

    /// <summary>
    /// The prefab used to make a planet with one or more moons. This top level gameobject's name is the
    /// same as the PlanetoidCategory of the planet. The immediate child is an orbit gameobject containing an
    /// orbit script. The child of the orbit contains the Planet Model and View, aka 'the planet'. Its children
    /// include 1) the atmosphere mesh, 2) the planet mesh, 3) a keepoutzone collider, and optionally one or more
    /// orbit gameobjects for moons. The orbit's child is the Moon Model and View. It's children are a moon mesh
    /// and a keepoutzone collider. 
    /// </summary>
    private GameObject[] _planetPrefabs;

    /// <summary>
    /// The prefab used to make a system. This top level gameobject's components include the System Model and
    /// VIew. It's child is an empty orbital plane object whose children include a plane mesh and highlight mesh. There 
    /// are other children and grandchildren but they are only transitional.
    /// </summary>
    private SystemModel _systemPrefab;

    private SystemFactory() {
        Initialize();
    }

    protected override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;
        _starPrefabs = reqdPrefabs.stars.Select(s => s.gameObject).ToArray();   // star prefabs are headed by the StarModel on a GO named after the star category
        _planetPrefabs = reqdPrefabs.planets;   // planet prefabs are headed by an empty GO named after the planet category
        _systemPrefab = reqdPrefabs.system;
    }

    /// <summary>
    /// Makes an instance of a Star based on the stat provided. The returned Model (with its Data) and View 
    /// will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    public StarModel MakeInstance(StarStat starStat, GameObject parent) {
        GameObject starPrefab = _starPrefabs.First(sGo => sGo.name == starStat.Category.GetName());
        GameObject starGo = UnityUtility.AddChild(parent, starPrefab);
        StarModel model = starGo.GetSafeMonoBehaviourComponent<StarModel>();
        MakeInstance(starStat, parent.name, ref model);
        return model;
    }

    /// <summary>
    /// Makes an instance of a Star from the stat and model provided. The Model (with its Data) and View will not be enabled.
    /// The model's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="model">The model.</param>
    public void MakeInstance(StarStat starStat, string systemName, ref StarModel model) {
        D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
        D.Assert(model.transform.parent != null, "{0} should already have a parent.".Inject(model.FullName));
        Transform transformContainingCategoryName = model.transform;
        D.Assert(starStat.Category == GameUtility.DeriveEnumFromName<StarCategory>(transformContainingCategoryName.name),
            "{0} {1} should = {2}.".Inject(typeof(StarCategory).Name, starStat.Category.GetName(), transformContainingCategoryName.name));
        model.Data = new StarData(starStat) {
            ParentName = systemName
        };

        // this is not really necessary as the provided model should already have its transform as its Mesh's CameraLOSChangedRelay target
        model.gameObject.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(model.transform);
    }

    /// <summary>
    /// Makes an instance of a Planet (with optionally attached moons) based on the stat provided. The returned 
    /// Model (with its Data) and View will not be enabled but their gameObject will be parented to the provided parent.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    public PlanetoidModel MakeInstance(PlanetoidStat planetStat, GameObject parent) {
        GameObject planetPrefab = _planetPrefabs.First(pGo => pGo.name == planetStat.Category.GetName());
        GameObject planetGo = UnityUtility.AddChild(parent, planetPrefab);

        IEnumerable<PlanetoidModel> allPlanetoidModels = planetGo.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>();
        // exclude moons
        PlanetoidModel planetModel = allPlanetoidModels.Single(pModel => pModel.gameObject.GetComponentInParents<PlanetoidModel>(excludeSelf: true) == null);

        MakeInstance(planetStat, parent.name, ref planetModel);
        return planetModel;
    }

    /// <summary>
    /// Makes an instance of a Planet (with optionally already attached moons) from the stat and model provided. 
    /// The Model (with its Data) and View will not be enabled. The model's transform will have the same parent it arrived with.
    /// </summary>
    /// <param name="planetStat">The planet stat.</param>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="model">The model.</param>
    public void MakeInstance(PlanetoidStat planetStat, string systemName, ref PlanetoidModel model) {
        D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
        D.Assert(model.transform.parent != null, "{0} should already have a parent.".Inject(model.FullName));
        Transform transformContainingCategoryName = model.transform.parent.parent;
        D.Assert(planetStat.Category == GameUtility.DeriveEnumFromName<PlanetoidCategory>(transformContainingCategoryName.name),
    "{0} {1} should = {2}.".Inject(typeof(PlanetoidCategory).Name, planetStat.Category.GetName(), transformContainingCategoryName.name));

        model.Data = new PlanetoidData(planetStat) {
            ParentName = systemName
        };

        // this is not really necessary as the provided model should already have its transform as its Mesh's CameraLOSChangedRelay target
        var modelTransform = model.transform;   // assigns the planet's transform as the target for the planet and moon mesh relays
        model.gameObject.GetInterfacesInChildren<ICameraLOSChangedRelay>().ForAll(iRelay => iRelay.AddTarget(modelTransform));
    }

    /// <summary>
    /// Makes an instance of a System from the name provided. The returned Model (with its Data) and View
    /// will not be enabled but their gameObject will be parented to the provided parent. Their are
    /// no subordinate planets or stars attached yet.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    public SystemModel MakeSystemInstance(string systemName, Index3D sectorIndex, SpaceTopography topography, GameObject parent) {
        GameObject systemPrefab = _systemPrefab.gameObject;
        GameObject systemGo = UnityUtility.AddChild(parent, systemPrefab);
        systemGo.name = systemName;
        SystemModel model = systemGo.GetSafeMonoBehaviourComponent<SystemModel>();
        MakeSystemInstance(systemName, sectorIndex, topography, ref model);
        return model;
    }

    /// <summary>
    /// Makes an instance of a System from the name and model provided. The Model (with its Data) and View
    /// will not be enabled. The model's transform will have the same parent and children it arrived with.
    /// </summary>
    /// <param name="systemName">Name of the system.</param>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="model">The model.</param>
    public void MakeSystemInstance(string systemName, Index3D sectorIndex, SpaceTopography topography, ref SystemModel model) {
        D.Assert(model.transform.parent != null, "{0} should already have a parent.".Inject(model.FullName));
        SystemData data = new SystemData(systemName, sectorIndex, topography);
        model.Data = data;
        // this is not really necessary as the provided model should already have its transform as its Mesh's CameraLOSChangedRelay target
        var modelTransform = model.transform;   // assigns the planet's transform as the target for the planet and moon mesh relays
        model.gameObject.GetInterfacesInChildren<ICameraLOSChangedRelay>().ForAll(iRelay => iRelay.AddTarget(modelTransform));
    }

}



