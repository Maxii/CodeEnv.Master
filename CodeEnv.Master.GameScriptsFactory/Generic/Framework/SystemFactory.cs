// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemFactory.cs
// Singleton. COMMENT - one line to give a brief idea of what the file does.
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
/// Singleton. COMMENT
/// </summary>
public class SystemFactory : AGenericSingleton<SystemFactory> {

    private GameObject[] _starPrefabs;
    private GameObject[] _planetPrefabs;    // includes moons

    private SystemFactory() {
        Initialize();
    }

    protected override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;

        _starPrefabs = reqdPrefabs.stars.Select(s => s.gameObject).ToArray();   // star prefabs are headed by the StarModel on a GO named after the star category
        _planetPrefabs = reqdPrefabs.planets;   // planet prefabs are headed by an empty GO named after the planet category
    }

    /// <summary>
    /// Makes an instance of a Star based on the stat provided. The Model and View will not be enabled,
    /// nor will their gameObject have a parent as the star has yet to be assigned to a System.
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <returns></returns>
    public StarModel MakeInstance(StarStat starStat) {
        GameObject starPrefab = _starPrefabs.First(sGo => sGo.name == starStat.Category.GetName());
        GameObject starGo = UnityUtility.AddChild(null, starPrefab);
        StarModel model = starGo.GetSafeMonoBehaviourComponent<StarModel>();
        MakeInstance(starStat, ref model);
        return model;
    }

    /// <summary>
    /// Makes an instance of a Star from the stat and model provided. The Model and View will not be enabled.
    /// The model's transform will have the same parent it arrived with, which could be null if the model was provided
    /// from MakeInstance(StarStat).
    /// </summary>
    /// <param name="starStat">The star stat.</param>
    /// <param name="model">The model.</param>
    public void MakeInstance(StarStat starStat, ref StarModel model) {
        D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
        model.Data = new StarData(starStat); // TODO how does a SystemName get assigned as the parentName of a star?

        // this is not really necessary as the provided model should already have its transform as its Mesh's CameraLOSChangedRelay target
        model.gameObject.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(model.transform);
    }

    // start with moons as part of a planet's prefab as current   // TODO separate moons from planet prefabs

    public PlanetoidModel MakeInstance(PlanetoidStat pStat) {
        GameObject planetPrefab = _planetPrefabs.First(pGo => pGo.name == pStat.Category.GetName());
        GameObject planetGo = UnityUtility.AddChild(null, planetPrefab);

        IEnumerable<PlanetoidModel> allPlanetoidModels = planetGo.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>();
        // excludes moons
        PlanetoidModel planetModel = allPlanetoidModels.Single(pModel => pModel.gameObject.GetComponentInParents<PlanetoidModel>(excludeSelf: true) == null);

        MakeInstance(pStat, ref planetModel);
        return planetModel;
    }

    public void MakeInstance(PlanetoidStat pStat, ref PlanetoidModel model) {
        D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
        model.Data = new PlanetoidData(pStat); // TODO how does a SystemName get assigned as the parentName of a planet, or a planet as parent of a moon?

        // this is not really necessary as the provided model should already have its transform as its Mesh's CameraLOSChangedRelay target
        var modelTransform = model.transform;   // assigns the planet's transform as the target for the planet and moon mesh relays
        model.gameObject.GetInterfacesInChildren<ICameraLOSChangedRelay>().ForAll(iRelay => iRelay.AddTarget(modelTransform));
    }

}



