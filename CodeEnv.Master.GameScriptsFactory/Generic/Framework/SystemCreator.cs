// --------------------------------------------------------------------------------------------------------------------
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
    private SystemItem _system;
    private StarItem _star;
    private IList<PlanetoidItem> _planets;
    private IEnumerable<PlanetoidItem> _moons;
    private SettlementItem _settlement; // can be null
    private IList<IDisposable> _subscribers;
    private bool _isExistingSystem;

    protected override void Awake() {
        base.Awake();
        _systemsFolder = _transform.parent;
        _isExistingSystem = _transform.childCount > 0;
        _systemName = _transform.name;  // the SystemManager will carry the name of the System
        CreateSystemComposition();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GameState.GeneratingPathGraphs) {
            DeploySystem();
            EnableSystem(); // must make View operational before starting state changes (like IntelLevel) within it 
        }
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_1) {
            __SetIntelLevel();
            DestroySystemManager();
        }
    }

    private void CreateSystemComposition() {
        if (_isExistingSystem) {
            CreateCompositionFromChildren();
        }
        else {
            GenerateOrbitSlotStartLocation();
            CreateRandomComposition();
        }
    }

    private void CreateCompositionFromChildren() {
        IEnumerable<PlanetoidItem> allPlanetoids = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidItem>();
        _planets = allPlanetoids.Where(p => p.gameObject.GetComponentInParents<PlanetoidItem>(excludeSelf: true) == null).ToList();
        _composition = new SystemComposition();
        foreach (var planet in _planets) {
            Transform transformCarryingPlanetType = planet.transform.parent.parent;
            PlanetoidType pType = GetType<PlanetoidType>(transformCarryingPlanetType);
            string planetName = planet.gameObject.name; // if already in scene, the planet should already be named for its system and orbit
            PlanetoidData data = CreatePlanetData(pType, planetName);
            _composition.AddPlanet(data);
        }

        _star = gameObject.GetSafeMonoBehaviourComponentInChildren<StarItem>();
        Transform transformCarryingStarType = _star.transform;
        StarType starType = GetType<StarType>(transformCarryingStarType);
        _composition.StarData = CreateStarData(starType);

        _settlement = gameObject.GetComponentInChildren<SettlementItem>();
        if (_settlement != null) {  // FIXME if provided system has no Settlement, the orbital slot reserved will be Vector3.zero
            Transform transformCarryingSettlementSize = _settlement.transform.parent.parent;
            SettlementSize size = GetType<SettlementSize>(transformCarryingSettlementSize);
            _composition.SettlementData = CreateSettlementData(size);
            _composition.SettlementOrbitSlot = _settlement.transform.localPosition;
        }
    }

    private void CreateRandomComposition() {
        _composition = new SystemComposition();

        //determine how many planets of which types for the system, then build PlanetoidData and add to composition
        int orbitSlotsAvailableForPlanets = _numberOfOrbitSlots - 1;    // 1 reserved for a Settlement 
        int planetCount = RandomExtended<int>.Range(0, orbitSlotsAvailableForPlanets);
        for (int i = 0; i < planetCount; i++) {

            IEnumerable<PlanetoidType> planetoidTypesToExclude = new PlanetoidType[] { default(PlanetoidType), 
                PlanetoidType.Moon_001, PlanetoidType.Moon_002, PlanetoidType.Moon_003, PlanetoidType.Moon_004, PlanetoidType.Moon_005 };
            PlanetoidType planetType = RandomExtended<PlanetoidType>.Choice(Enums<PlanetoidType>.GetValues().Except(planetoidTypesToExclude).ToArray());

            string planetName = "{0}, rest deferred until orbit assigned.".Inject(planetType.GetName());
            PlanetoidData planetData = CreatePlanetData(planetType, planetName);
            _composition.AddPlanet(planetData);
        }

        StarType starType = Enums<StarType>.GetRandom(excludeDefault: true);
        StarData starData = CreateStarData(starType);
        _composition.StarData = starData;

        SettlementSize size = Enums<SettlementSize>.GetRandom(excludeDefault: false);   // allows no Settlement
        if (size != SettlementSize.None) {
            SettlementData settlementData = CreateSettlementData(size);
            _composition.SettlementData = settlementData;
        }
    }

    private PlanetoidData CreatePlanetData(PlanetoidType pType, string planetName) {
        PlanetoidData data = new PlanetoidData(pType, planetName, 100000F, _systemName) {
            Capacity = 25,
            Resources = new OpeYield(3.1F, 2.0F, 4.8F),
            SpecialResources = new XYield(XResource.Special_1, 0.3F),
            LastHumanPlayerIntelDate = new GameDate(),
        };
        return data;
    }

    private StarData CreateStarData(StarType sType) {
        string starName = _systemName + Constants.Space + CommonTerms.Star;
        StarData data = new StarData(sType, starName, _systemName) {
            Capacity = 100,
            Resources = new OpeYield(0F, 0F, 100F),
            SpecialResources = new XYield(XResource.Special_3, 0.3F),
            LastHumanPlayerIntelDate = new GameDate()
        };
        return data;
    }

    private SettlementData CreateSettlementData(SettlementSize size) {
        string settlementName = _systemName + Constants.Space + size.GetName();
        SettlementData data = new SettlementData(size, settlementName, 50F, _systemName) {
            Population = 100,
            CapacityUsed = 10,
            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F)),
            Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
            CurrentHitPoints = 38F,
            Owner = new Player()
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
        _system = gameObject.GetSafeMonoBehaviourComponentInChildren<SystemItem>();
        InitializePlanets();
        InitializeMoons();
        InitializeStar();
        InitializeSettlement();
        InitializeSystem();
    }

    private void DeployRandomSystem() {
        BuildSystem();  // first as planets, stars and settlements need a system parent
        BuildPlanets();
        BuildStar();
        BuildSettlement();
        InitializePlanets();
        AssignElementsToOrbitSlots(); // after InitPlanets so planet names can be changed according to orbit
        InitializeMoons();
        InitializeStar();
        InitializeSettlement();
        InitializeSystem();
    }

    private void BuildSystem() {
        GameObject systemGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.system.gameObject);
        systemGo.name = _systemName;     // assign the name of the system
        _system = systemGo.GetSafeMonoBehaviourComponent<SystemItem>();
    }

    private void BuildPlanets() {
        _planets = new List<PlanetoidItem>();
        foreach (var pType in _composition.PlanetTypes) {
            GameObject planetPrefab = RequiredPrefabs.Instance.planets.First(p => p.gameObject.name == pType.GetName());
            string nameOfPlanetType = planetPrefab.name;
            GameObject systemGo = _system.gameObject;

            _composition.GetPlanetData(pType).ForAll(pd => {
                GameObject topLevelPlanetGo = UnityUtility.AddChild(systemGo, planetPrefab);
                topLevelPlanetGo.name = nameOfPlanetType; // get rid of (Clone) in name as the name holds the PlanetoidType which is needed in InitPlanets
                PlanetoidItem planet = topLevelPlanetGo.GetSafeMonoBehaviourComponentInChildren<PlanetoidItem>();
                _planets.Add(planet);
            });
        }
    }

    private void BuildStar() {
        StarType starType = _composition.StarData.StarType;
        GameObject starPrefab = RequiredPrefabs.Instance.stars.First(sp => sp.gameObject.name == starType.GetName()).gameObject;
        GameObject systemGo = _system.gameObject;
        GameObject starGo = UnityUtility.AddChild(systemGo, starPrefab);
        _star = starGo.GetSafeMonoBehaviourComponent<StarItem>();
    }

    private void BuildSettlement() {
        SettlementData data = _composition.SettlementData;
        if (data != null) {
            SettlementSize size = data.SettlementSize;
            GameObject settlementPrefab = RequiredPrefabs.Instance.settlements.First(sp => sp.gameObject.name == size.GetName());
            string nameOfSettlementSize = settlementPrefab.name;
            GameObject systemGo = _system.gameObject;
            GameObject topLevelSettlementGo = UnityUtility.AddChild(systemGo, settlementPrefab);
            topLevelSettlementGo.name = nameOfSettlementSize;   // get rid of (Clone)
            _settlement = topLevelSettlementGo.GetSafeMonoBehaviourComponentInChildren<SettlementItem>();
        }
    }

    private void InitializePlanets() {
        IDictionary<PlanetoidType, Stack<PlanetoidData>> typeLookup = new Dictionary<PlanetoidType, Stack<PlanetoidData>>();
        foreach (var planet in _planets) {
            Transform transformCarryingPlanetType = planet.transform.parent.parent; // top level planetarySystem go holding orbits, planet and moons
            PlanetoidType pType = GetType<PlanetoidType>(transformCarryingPlanetType);

            Stack<PlanetoidData> dataStack;
            if (!typeLookup.TryGetValue(pType, out dataStack)) {
                dataStack = new Stack<PlanetoidData>(_composition.GetPlanetData(pType));
                typeLookup.Add(pType, dataStack);
            }
            planet.Data = dataStack.Pop();  // automatically adds the planet's transform to Data when set
            // include the System and planet as a target in any child with a CameraLOSChangedRelay
            planet.gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(r => r.AddTarget(_system.transform, planet.transform));
        }
    }

    private void InitializeMoons() {
        _moons = new List<PlanetoidItem>();
        foreach (var planet in _planets) {
            IEnumerable<PlanetoidItem> moons = planet.gameObject.GetComponentsInChildren<PlanetoidItem>().Except(planet);
            if (!moons.IsNullOrEmpty()) {
                int letterIndex = 0;
                foreach (var moon in moons) {
                    string planetName = planet.Data.Name;
                    string moonName = planetName + _moonLetters[letterIndex];
                    PlanetoidType moonType = GetType<PlanetoidType>(moon.transform);
                    PlanetoidData data = new PlanetoidData(moonType, moonName, 10000F, _systemName) {
                        LastHumanPlayerIntelDate = new GameDate()
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
        // PlayerIntelLevel is determined by the IntelLevel of the System
    }

    private void InitializeSettlement() {
        if (_settlement != null) {
            _settlement.Data = _composition.SettlementData;
            // PlayerIntelLevel is determined by the IntelLevel of the System
            // SystemData gets settlement data assigned to it when it is created via the composition
            // include the System and Settlement as a target in any child with a CameraLOSChangedRelay
            _settlement.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_system.transform, _settlement.transform);
        }
    }

    /// <summary>
    /// Assigns each planet and/or a Settlement to an appropriate orbit slot. If an appropriate orbit slot is no
    /// longer available for a planet, the planet is removed and destroyed.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void AssignElementsToOrbitSlots() {
        int innerOrbitsCount = Mathf.FloorToInt(0.25F * _numberOfOrbitSlots);
        int midOrbitsCount = Mathf.CeilToInt(0.6F * _numberOfOrbitSlots) - innerOrbitsCount;
        int outerOrbitsCount = _numberOfOrbitSlots - innerOrbitsCount - midOrbitsCount;
        var innerStack = new Stack<int>(Enumerable.Range(0, innerOrbitsCount).Shuffle());
        var midStack = new Stack<int>(Enumerable.Range(innerOrbitsCount, midOrbitsCount).Shuffle());
        var outerStack = new Stack<int>(Enumerable.Range(innerOrbitsCount + midOrbitsCount, outerOrbitsCount).Shuffle());

        // start by reserving and possibly assigning the slot for the Settlement
        int slotIndex = midStack.Pop();
        if (_settlement != null) {
            _settlement.transform.localPosition = _orbitSlots[slotIndex];
        }
        _composition.SettlementOrbitSlot = _orbitSlots[slotIndex];

        // now divy up the remaining slots among the planets
        IList<PlanetoidItem> planetsToDestroy = null;
        Stack<int>[] slots;
        foreach (var planet in _planets) {
            var pType = planet.Data.PlanetoidType;
            switch (pType) {
                case PlanetoidType.Volcanic:
                    slots = new Stack<int>[] { innerStack, midStack };
                    break;
                case PlanetoidType.Terrestrial:
                    slots = new Stack<int>[] { midStack, innerStack, outerStack };
                    break;
                case PlanetoidType.Ice:
                    slots = new Stack<int>[] { outerStack, midStack };
                    break;
                case PlanetoidType.GasGiant:
                    slots = new Stack<int>[] { outerStack, midStack };
                    break;
                case PlanetoidType.Moon_001:
                case PlanetoidType.Moon_002:
                case PlanetoidType.Moon_003:
                case PlanetoidType.Moon_004:
                case PlanetoidType.Moon_005:
                case PlanetoidType.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pType));
            }

            if (TryFindOrbitSlot(out slotIndex, slots)) {
                planet.transform.localPosition = _orbitSlots[slotIndex];
                // assign the planet's name using its orbital slot
                planet.Data.Name = _systemName + Constants.Space + _planetNumbers[slotIndex];
            }
            else {
                if (planetsToDestroy == null) {
                    planetsToDestroy = new List<PlanetoidItem>();
                }
                planetsToDestroy.Add(planet);
            }
        }
        if (planetsToDestroy != null) {
            planetsToDestroy.ForAll(p => {
                _planets.Remove(p);
                _composition.RemovePlanet(p.Data);
                D.Log("Destroying Planet {0}.", p.gameObject.name);
                Destroy(p);
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
        SystemData data = new SystemData(_systemName, _composition) {  // the data in composition should stay current as data values are changed
            // there is no parentName for a System
            LastHumanPlayerIntelDate = new GameDate(),
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
        if (_settlement != null) { _settlement.enabled = true; }

        _planets.ForAll(p => p.gameObject.GetSafeMonoBehaviourComponent<PlanetoidView>().enabled = true);
        _moons.ForAll(m => m.gameObject.GetSafeMonoBehaviourComponent<PlanetoidView>().enabled = true);
        _system.gameObject.GetSafeMonoBehaviourComponent<SystemView>().enabled = true;
        _star.gameObject.GetSafeMonoBehaviourComponent<StarView>().enabled = true;
        if (_settlement != null) {
            _settlement.gameObject.GetSafeMonoBehaviourComponent<SettlementView>().enabled = true;
        }
    }

    private void __SetIntelLevel() {
        _system.gameObject.GetSafeInterface<ISystemViewable>().PlayerIntelLevel = IntelLevel.Complete; // Enums<IntelLevel>.GetRandom(excludeDefault: true);
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

    private T GetType<T>(Transform transformWithTypeName) where T : struct {
        return Enums<T>.Parse(transformWithTypeName.name);
    }

    private void DestroySystemManager() {
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

