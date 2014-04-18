﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreator.cs
//  Initialization class that deploys a system at the location of this SystemCreator. 
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
/// Initialization class that deploys a system at the location of this SystemCreator. The system
/// deployed will simply be initialized if already present in the scene. If it is not present, then
/// it will be built and then initialized.
/// <remarks>Naming approach: 
/// <list type="bullet" >
/// <item>
///     <description>System: The name of the system is delivered by this SystemCreator using the name of its gameobject.  </description>
/// </item>
/// <item>
///     <description>Planets: The planetary system (planet, orbits, moons) gameobject will always hold the name
/// of the planet type. It should not change. The name of the planet gameobject itself must already be set 
/// if the planet is already in the scene. Otherwise, planet names will be automatically constructed using the name of the
/// system and the orbital slot they end up being assigned (eg. Regulas 1).</description>
/// </item>
/// /// <item>
///     <description>Stars, Settlements and Moons: Likewise, names of Stars, Settlements and Moons
/// will automatically be assigned when initialized, always bearing the name of the system. (eg. [SystemName] Star, 
/// [SystemName] City, [PlanetName]a indicating a moon </description>
/// </item>
///  </remarks>
/// </summary>
public class SystemCreator : AMonoBase, IDisposable {

    private static int _numberOfOrbitSlots = TempGameValues.SystemOrbitSlots;

    private static int[] _planetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static string[] _moonLetters = new string[] { "a", "b", "c", "d", "e" };

    private Vector3[] _orbitSlots;

    private string _systemName;
    private Transform _systemsFolder;

    private SystemComposition _composition;
    private SystemModel _system;
    private StarModel _star;
    private IList<PlanetoidModel> _planets;
    private IEnumerable<PlanetoidModel> _moons;
    private Vector3 _settlementOrbitSlot;

    // Removed Settlement treatment. Now built separately like a Starbase and assigned to a system

    private IList<IDisposable> _subscribers;
    private bool _isExistingSystem;

    protected override void Awake() {
        base.Awake();
        _systemsFolder = _transform.parent;
        _isExistingSystem = _transform.childCount > 0;
        _systemName = _transform.name;  // the SystemCreator will carry the name of the System
        RegisterGameStateProgressionReadiness(isReady: false);
        CreateSystemComposition();
        Subscribe();
    }

    private void RegisterGameStateProgressionReadiness(bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, GameState.DeployingSystems, isReady));
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GameState.DeployingSystems) {
            DeploySystem();
            EnableSystem(); // must make View operational before starting state changes (like IntelLevel) within it 
            RegisterGameStateProgressionReadiness(isReady: true);
        }
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_2) {
            __SetIntelLevel();
        }
        if (GameManager.Instance.CurrentState == GameState.Running) {
            DestroyCreationObject(); // destruction deferred so __UniverseInitializer can complete its work
        }
    }

    private void CreateSystemComposition() {
        GenerateOrbitSlotStartLocation();
        if (_isExistingSystem) {
            CreateCompositionFromChildren();
        }
        else {
            CreateRandomComposition();
        }
    }

    private void CreateCompositionFromChildren() {
        IEnumerable<PlanetoidModel> allPlanetoids = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>();
        _planets = allPlanetoids.Where(p => p.gameObject.GetComponentInParents<PlanetoidModel>(excludeSelf: true) == null).ToList();
        _composition = new SystemComposition();
        foreach (var planet in _planets) {
            Transform transformCarryingPlanetCategory = planet.transform.parent.parent;
            PlanetoidCategory pCategory = DeriveCategory<PlanetoidCategory>(transformCarryingPlanetCategory);
            string planetName = planet.gameObject.name; // if already in scene, the planet should already be named for its system and orbit
            PlanetoidData data = CreatePlanetData(pCategory, planetName);
            _composition.AddPlanet(data);
        }

        _star = gameObject.GetSafeMonoBehaviourComponentInChildren<StarModel>();
        Transform transformCarryingStarCategory = _star.transform;
        StarCategory starCategory = DeriveCategory<StarCategory>(transformCarryingStarCategory);
        _composition.StarData = CreateStarData(starCategory);
    }

    private void CreateRandomComposition() {
        _composition = new SystemComposition();

        //determine how many planets of which types for the system, then build PlanetoidData and add to composition
        int orbitSlotsAvailableForPlanets = _numberOfOrbitSlots - 1;    // 1 reserved for a Settlement 
        int planetCount = RandomExtended<int>.Range(0, orbitSlotsAvailableForPlanets);
        for (int i = 0; i < planetCount; i++) {

            IEnumerable<PlanetoidCategory> planetoidCategoriesToExclude = new PlanetoidCategory[] { default(PlanetoidCategory), 
                PlanetoidCategory.Moon_001, PlanetoidCategory.Moon_002, PlanetoidCategory.Moon_003, PlanetoidCategory.Moon_004, PlanetoidCategory.Moon_005 };
            PlanetoidCategory planetCategory = RandomExtended<PlanetoidCategory>.Choice(Enums<PlanetoidCategory>.GetValues().Except(planetoidCategoriesToExclude).ToArray());

            string planetName = "{0}, rest deferred until orbit assigned.".Inject(planetCategory.GetName());
            PlanetoidData planetData = CreatePlanetData(planetCategory, planetName);
            _composition.AddPlanet(planetData);
        }

        StarCategory starCategory = Enums<StarCategory>.GetRandom(excludeDefault: true);
        StarData starData = CreateStarData(starCategory);
        _composition.StarData = starData;
    }

    private PlanetoidData CreatePlanetData(PlanetoidCategory pCategory, string planetName) {
        PlanetoidData data = new PlanetoidData(pCategory, planetName, 10000F, 1000000F, _systemName) {
            Strength = new CombatStrength(),
            Owner = TempGameValues.NoPlayer,
            Capacity = 25,
            Resources = new OpeYield(3.1F, 2.0F, 4.8F),
            SpecialResources = new XYield(XResource.Special_1, 0.3F),
        };
        return data;
    }

    private StarData CreateStarData(StarCategory sCategory) {
        string starName = _systemName + Constants.Space + CommonTerms.Star;
        StarData data = new StarData(sCategory, starName, _systemName) {
            Capacity = 100,
            Resources = new OpeYield(0F, 0F, 100F),
            SpecialResources = new XYield(XResource.Special_3, 0.3F),
        };
        return data;
    }

    private void DeploySystem() {
        if (_isExistingSystem) {
            DeployExistingSystem();
        }
        else {
            DeployRandomSystem();
        }
    }

    private void DeployExistingSystem() {
        _system = gameObject.GetSafeMonoBehaviourComponentInChildren<SystemModel>();
        InitializePlanets();
        AssignElementsToOrbitSlots();   // repositions planet orbits and changes name to reflect slot
        InitializeMoons();
        InitializeStar();
        InitializeSystem();
    }

    private void DeployRandomSystem() {
        BuildSystem();  // first as planets, stars and settlements need a system parent
        BuildPlanets();
        BuildStar();
        InitializePlanets();
        AssignElementsToOrbitSlots(); // after InitPlanets so planet names can be changed according to orbit
        InitializeMoons();
        InitializeStar();
        InitializeSystem();
    }

    private void BuildSystem() {
        GameObject systemGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.system.gameObject);
        systemGo.name = _systemName;     // assign the name of the system
        _system = systemGo.GetSafeMonoBehaviourComponent<SystemModel>();
    }

    private void BuildPlanets() {
        _planets = new List<PlanetoidModel>();
        foreach (var pCat in _composition.PlanetCategories) {
            GameObject planetPrefab = RequiredPrefabs.Instance.planets.First(p => p.gameObject.name == pCat.GetName());
            GameObject systemGo = _system.gameObject;

            _composition.GetPlanetData(pCat).ForAll(pd => {
                GameObject topLevelPlanetGo = UnityUtility.AddChild(systemGo, planetPrefab);
                PlanetoidModel planet = topLevelPlanetGo.GetSafeMonoBehaviourComponentInChildren<PlanetoidModel>();
                _planets.Add(planet);
            });
        }
    }

    private void BuildStar() {
        StarCategory starCat = _composition.StarData.Category;
        GameObject starPrefab = RequiredPrefabs.Instance.stars.First(sp => sp.gameObject.name == starCat.GetName()).gameObject;
        GameObject systemGo = _system.gameObject;
        GameObject starGo = UnityUtility.AddChild(systemGo, starPrefab);
        _star = starGo.GetSafeMonoBehaviourComponent<StarModel>();
    }

    private void InitializePlanets() {
        IDictionary<PlanetoidCategory, Stack<PlanetoidData>> planetCategoryLookup = new Dictionary<PlanetoidCategory, Stack<PlanetoidData>>();
        foreach (var planet in _planets) {
            Transform transformCarryingPlanetType = planet.transform.parent.parent; // top level planetarySystem go holding orbits, planet and moons
            PlanetoidCategory pCategory = DeriveCategory<PlanetoidCategory>(transformCarryingPlanetType);

            Stack<PlanetoidData> dataStack;
            if (!planetCategoryLookup.TryGetValue(pCategory, out dataStack)) {
                dataStack = new Stack<PlanetoidData>(_composition.GetPlanetData(pCategory));
                planetCategoryLookup.Add(pCategory, dataStack);
            }
            planet.Data = dataStack.Pop();  // automatically adds the planet's transform to Data when set
            // include the System and planet as a target in any child with a CameraLOSChangedRelay
            planet.gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(r => r.AddTarget(_system.transform, planet.transform));
        }
    }

    private void InitializeMoons() {
        _moons = new List<PlanetoidModel>();
        foreach (var planet in _planets) {
            IEnumerable<PlanetoidModel> moons = planet.gameObject.GetComponentsInChildren<PlanetoidModel>().Except(planet);
            if (!moons.IsNullOrEmpty()) {
                int letterIndex = 0;
                foreach (var moon in moons) {
                    string planetName = planet.Data.Name;
                    string moonName = planetName + _moonLetters[letterIndex];
                    PlanetoidCategory moonCategory = DeriveCategory<PlanetoidCategory>(moon.transform);
                    PlanetoidData data = new PlanetoidData(moonCategory, moonName, 1000F, 100000F, _systemName) {
                        Strength = new CombatStrength(),
                        Owner = TempGameValues.NoPlayer,
                        Capacity = 5,
                        Resources = new OpeYield(0.1F, 1.0F, 0.8F),
                        SpecialResources = null,
                    };

                    moon.Data = data;
                    letterIndex++;
                    // include the System and moon as a target in any child with a CameraLOSChangedRelay
                    moon.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_system.transform, moon.transform);
                }
                _moons = _moons.Concat(moons);
            }
        }
    }

    private void InitializeStar() {
        _star.Data = _composition.StarData; // automatically assigns the transform to data and aligns the transform's name to held by data
        // include the System and Star as a target in any child with a CameraLOSChangedRelay
        _star.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_system.transform, _star.transform);
        // PlayerIntel coverage is fixed at Comprehensive
    }

    /// <summary>
    /// Assigns each element to an appropriate orbit slot including reserving a slot for a future settlement.
    /// If an appropriate orbit slot is no longer available for a planet, the planet is removed and destroyed.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void AssignElementsToOrbitSlots() {
        int innerOrbitsCount = Mathf.FloorToInt(0.25F * _numberOfOrbitSlots);
        int midOrbitsCount = Mathf.CeilToInt(0.6F * _numberOfOrbitSlots) - innerOrbitsCount;
        int outerOrbitsCount = _numberOfOrbitSlots - innerOrbitsCount - midOrbitsCount;
        var innerStack = new Stack<int>(Enumerable.Range(0, innerOrbitsCount).Shuffle());
        var midStack = new Stack<int>(Enumerable.Range(innerOrbitsCount, midOrbitsCount).Shuffle());
        var outerStack = new Stack<int>(Enumerable.Range(innerOrbitsCount + midOrbitsCount, outerOrbitsCount).Shuffle());

        // start by reserving the slot for the Settlement
        int slotIndex = midStack.Pop();
        _settlementOrbitSlot = _orbitSlots[slotIndex];

        // now divy up the remaining slots among the planets
        IList<PlanetoidModel> planetsToDestroy = null;
        Stack<int>[] slots;
        foreach (var planet in _planets) {
            var planetCategory = planet.Data.Category;
            switch (planetCategory) {
                case PlanetoidCategory.Volcanic:
                    slots = new Stack<int>[] { innerStack, midStack };
                    break;
                case PlanetoidCategory.Terrestrial:
                    slots = new Stack<int>[] { midStack, innerStack, outerStack };
                    break;
                case PlanetoidCategory.Ice:
                    slots = new Stack<int>[] { outerStack, midStack };
                    break;
                case PlanetoidCategory.GasGiant:
                    slots = new Stack<int>[] { outerStack, midStack };
                    break;
                case PlanetoidCategory.Moon_001:
                case PlanetoidCategory.Moon_002:
                case PlanetoidCategory.Moon_003:
                case PlanetoidCategory.Moon_004:
                case PlanetoidCategory.Moon_005:
                case PlanetoidCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(planetCategory));
            }

            if (TryFindOrbitSlot(out slotIndex, slots)) {
                planet.transform.localPosition = _orbitSlots[slotIndex];
                // assign the planet's name using its orbital slot
                planet.Data.Name = _systemName + Constants.Space + _planetNumbers[slotIndex];
            }
            else {
                if (planetsToDestroy == null) {
                    planetsToDestroy = new List<PlanetoidModel>();
                }
                planetsToDestroy.Add(planet);
            }
        }
        if (planetsToDestroy != null) {
            planetsToDestroy.ForAll(p => {
                _planets.Remove(p);
                _composition.RemovePlanet(p.Data);
                D.Log("Destroying Planet {0}.", p.gameObject.name);
                Destroy(p.gameObject);
            });
        }
    }

    private bool TryFindOrbitSlot(out int slot, params Stack<int>[] slotStacks) {
        foreach (var slotStack in slotStacks) {
            if (slotStack.Count > 0) {
                slot = slotStack.Pop();
                return true;
            }
        }
        slot = -1;
        return false;
    }

    private void InitializeSystem() {
        SystemData data = new SystemData(_systemName, _composition) {
            SettlementOrbitSlot = _settlementOrbitSlot
        };
        _system.Data = data;
        // include the System as a target in any child with a CameraLOSChangedRelay
        _system.gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(r => r.AddTarget(_system.transform));
    }

    private void EnableSystem() {
        _planets.ForAll(p => p.enabled = true);
        _moons.ForAll(m => m.enabled = true);
        _system.enabled = true;
        _star.enabled = true;
        // Models now enable their corresponding View after they initialize
    }

    private void __SetIntelLevel() {
        _system.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        _planets.ForAll(p => p.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive);
    }

    private void GenerateOrbitSlotStartLocation() {
        _orbitSlots = new Vector3[_numberOfOrbitSlots];
        float systemRadiusAvailableForOrbits = TempGameValues.SystemRadius - TempGameValues.StarKeepoutRadius;
        float slotSpacing = systemRadiusAvailableForOrbits / _numberOfOrbitSlots;
        for (int i = 0; i < _numberOfOrbitSlots; i++) {
            float orbitRadius = TempGameValues.StarKeepoutRadius + slotSpacing * (i + 1);
            Vector2 startOrbitPoint2D = RandomExtended<float>.OnCircle(orbitRadius);
            _orbitSlots[i] = new Vector3(startOrbitPoint2D.x, 0F, startOrbitPoint2D.y);
        }
    }

    private T DeriveCategory<T>(Transform transformContainingCategoryName) where T : struct {
        return Enums<T>.Parse(transformContainingCategoryName.name);
    }

    private void DestroyCreationObject() {
        foreach (Transform child in _transform) {
            child.parent = _systemsFolder;
        }
        Destroy(gameObject);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

