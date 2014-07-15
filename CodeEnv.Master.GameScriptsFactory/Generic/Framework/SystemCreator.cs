// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreator.cs
// Creates a system at the location of this script's GameObject. 
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
/// Creates a system at the location of this script's GameObject. The system
/// deployed will be fleshed out and operationalized if already present in the scene. If it is not present, then
/// it will first be built, then fleshed out and operationalized.
/// <remarks>Naming approach: 
/// <list type="bullet" >
/// <item>
///     <description>System: The name of the system is delivered by this SystemCreator using the name of its gameobject.  </description>
/// </item>
/// <item>
///     <description>Stars, Settlements, Planets and Moons: All names are automatically constructed using the name of the
/// system or planet, and the orbit slot they end up residing within (eg. Regulas 1a).</description>
/// </item>
///  </remarks>
/// </summary>
[SerializeAll]
public class SystemCreator : AMonoBase, IDisposable {

    public static IList<SystemModel> AllSystems { get { return _systemLookupBySectorIndex.Values.ToList(); } }

    /// <summary>
    /// Returns true if the sector indicated by sectorIndex contains a System.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="system">The system if present in the sector.</param>
    /// <returns></returns>
    public static bool TryGetSystem(Index3D sectorIndex, out SystemModel system) {
        return _systemLookupBySectorIndex.TryGetValue(sectorIndex, out system);
    }

    private static IDictionary<Index3D, SystemModel> _systemLookupBySectorIndex = new Dictionary<Index3D, SystemModel>();

    private static int[] _planetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static string[] _moonLetters = new string[] { "a", "b", "c", "d", "e" };

    private static IEnumerable<PlanetoidCategory> _acceptablePlanetCategories = new PlanetoidCategory[] { 
        PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial, PlanetoidCategory.Volcanic 
    };

    private static IEnumerable<PlanetoidCategory> _acceptableMoonCategories = new PlanetoidCategory[] { 
        PlanetoidCategory.Moon_001, PlanetoidCategory.Moon_002, PlanetoidCategory.Moon_003, PlanetoidCategory.Moon_004, PlanetoidCategory.Moon_005
    };

    // these must be set from Awake() 
    private static GameTimeDuration _minSystemOrbitPeriod;
    private static GameTimeDuration _systemOrbitPeriodIncrement; 
    private static GameTimeDuration _minMoonOrbitPeriod; 
    private static GameTimeDuration _moonOrbitPeriodIncrement; 

    public bool isCompositionPreset;
    public int maxRandomPlanets = 3;
    public int maxRandomMoons = 3;

    public string SystemName { get { return _transform.name; } }    // the SystemCreator carries the name of the System

    private float _systemOrbitSlotDepth;

    private StarStat _starStat;
    private IList<PlanetoidStat> _planetStats;
    private IList<PlanetoidStat> _moonStats;

    private SystemModel _system;
    private StarModel _star;
    private IList<PlanetModel> _planets;
    private IList<MoonModel> _moons;

    private Transform _systemsFolder;

    private IList<IDisposable> _subscribers;
    private SystemFactory _factory;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        _systemsFolder = _transform.parent;
        _factory = SystemFactory.Instance;
        D.Assert(isCompositionPreset == _transform.childCount > 0, "{0}.{1} Composition Preset flag is incorrect.".Inject(SystemName, GetType().Name));
        SetStaticValues();
        Subscribe();
    }

    /// <summary>
    /// Sets static values that cannot be set via a static initializer.
    /// 
    /// WARNING: Static initializers use the Loading Thread which runs AT EDITOR TIME.
    /// GameTimeDuration needs to load values from XML (GeneralSettings) which won't
    /// run until the Editor.Play button is pressed. Unity avoids this condition by requiring
    /// that calls to Application.dataPath (via UnityConstants.DataLibraryDir, AValuesHelper
    /// and GeneralSettings) come from the MainThread, not the LoadingThread.
    /// If from the LoadingThread, an ArgumentException is raised.
    /// </summary>
    private void SetStaticValues() {
        if (_minSystemOrbitPeriod == default(GameTimeDuration)) {
            _minSystemOrbitPeriod = GameTimeDuration.OneYear;
        }
        if (_systemOrbitPeriodIncrement == default(GameTimeDuration)) {
            _systemOrbitPeriodIncrement = new GameTimeDuration(hours: 0, days: GameTime.DaysPerYear / 2, years: 0);
        }
        if (_minMoonOrbitPeriod == default(GameTimeDuration)) {
            _minMoonOrbitPeriod = new GameTimeDuration(hours: 0, days: 20, years: 0);
        }
        if (_moonOrbitPeriodIncrement == default(GameTimeDuration)) {
            _moonOrbitPeriodIncrement = new GameTimeDuration(hours: 0, days: 10, years: 0);
        }
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanging));
    }

    private void OnGameStateChanging(GameState newGameState) {
        GameState previousGameState = _gameMgr.CurrentState;
        if (previousGameState == GameState.BuildAndDeploySystems) {
            _systemLookupBySectorIndex.Add(_system.Data.SectorIndex, _system);
        }
    }

    private void OnGameStateChanged() {
        GameState gameState = _gameMgr.CurrentState;
        BuildDeployAndBeginSystemOperationsDuringStartup(gameState);
    }

    private void BuildDeployAndBeginSystemOperationsDuringStartup(GameState gameState) {
        if (gameState == GameState.BuildAndDeploySystems) {
            RegisterGameStateProgressionReadiness(isReady: false);
            CreateStats();
            PrepareForOperations();
            EnableSystem(onCompletion: delegate {
                __SetIntelLevel();
                RegisterGameStateProgressionReadiness(isReady: true);
            });
            // System is now prepared to receive a Settlement when it deploys
        }

        if (gameState == GameState.Running) {
            EnableOtherWhenRunning();
            BeginSystemOperations(onCompletion: delegate {
                // wait to allow any cellestial objects using the IEnumerator StateMachine to enter their starting state
                DestroyCreationObject(); // destruction deferred so __UniverseInitializer can complete its work
            });
        }
    }

    #region Create Stats

    private void CreateStats() {
        _starStat = CreateStarStat();
        _planetStats = CreatePlanetStats();
        _moonStats = CreateMoonStats();
    }

    private StarStat CreateStarStat() {
        LogEvent();
        StarCategory category;
        if (isCompositionPreset) {
            category = gameObject.GetSafeMonoBehaviourComponentInChildren<StarModel>().category;
        }
        else {
            category = Enums<StarCategory>.GetRandom(excludeDefault: true);
        }
        return new StarStat(category, 100, new OpeYield(0F, 0F, 100F), new XYield(XResource.Special_3, 0.3F));
    }

    private IList<PlanetoidStat> CreatePlanetStats() {
        LogEvent();
        var planetStats = new List<PlanetoidStat>();
        if (isCompositionPreset) {
            var planets = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetModel>();
            foreach (var planet in planets) {
                PlanetoidCategory pCategory = planet.category;
                PlanetoidStat stat = new PlanetoidStat(1000000F, 100F, pCategory, 25, new OpeYield(3.1F, 2F, 4.8F), new XYield(XResource.Special_1, 0.3F));
                planetStats.Add(stat);
            }
        }
        else {
            int planetCount = maxRandomPlanets;
            D.Log("{0} random planet count = {1}.", SystemName, planetCount);
            for (int i = 0; i < planetCount; i++) {
                PlanetoidCategory pCategory = RandomExtended<PlanetoidCategory>.Choice(_acceptablePlanetCategories);
                PlanetoidStat stat = new PlanetoidStat(1000000F, 100F, pCategory, 25, new OpeYield(3.1F, 2F, 4.8F), new XYield(XResource.Special_1, 0.3F));
                planetStats.Add(stat);
            }
        }
        return planetStats;
    }

    private IList<PlanetoidStat> CreateMoonStats() {
        LogEvent();
        var moonStats = new List<PlanetoidStat>();
        if (isCompositionPreset) {
            var moons = gameObject.GetComponentsInChildren<MoonModel>();
            foreach (var moon in moons) {
                var mCategory = moon.category;
                PlanetoidStat stat = new PlanetoidStat(10000F, 10F, mCategory, 5, new OpeYield(0.1F, 1F, 0.8F));
                moonStats.Add(stat);
            }
        }
        else {
            int moonCount = maxRandomMoons;
            D.Log("{0} random moon count = {1}.", SystemName, moonCount);
            for (int i = 0; i < moonCount; i++) {
                PlanetoidCategory mCategory = RandomExtended<PlanetoidCategory>.Choice(_acceptableMoonCategories);
                PlanetoidStat stat = new PlanetoidStat(10000F, 10F, mCategory, 5, new OpeYield(0.1F, 1F, 0.8F));
                moonStats.Add(stat);
            }
        }
        return moonStats;
    }

    #endregion

    private void PrepareForOperations() {
        LogEvent();
        MakeSystem();   // stars and planets need a system parent when built
        MakeStar();     // makes the star a child of the system
        MakePlanets();  // makes each planet a child of the system
        AssignSystemOrbitSlotsToPlanets();    // modifies planet names to reflect the assigned orbit
        MakeMoons();    // makes each moon a child of a planet
        AssignPlanetOrbitSlotsToMoons();    // modifies moon names based on its assigned planetary orbit
        AddMembersToSystemData();         // adds star and planet data to the system's data component
        InitializeTopographyMonitor();
        CompleteSystem();               // misc final touchup
    }

    private void MakeSystem() {
        LogEvent();
        Index3D sectorIndex = SectorGrid.GetSectorIndex(_transform.position);
        if (isCompositionPreset) {
            _system = gameObject.GetSafeMonoBehaviourComponentInChildren<SystemModel>();
            _factory.MakeSystemInstance(SystemName, sectorIndex, SpaceTopography.OpenSpace, ref _system);
        }
        else {
            _system = _factory.MakeSystemInstance(sectorIndex, SpaceTopography.OpenSpace, this);
        }
    }

    private void MakeStar() {
        LogEvent();
        if (isCompositionPreset) {
            _star = gameObject.GetSafeMonoBehaviourComponentInChildren<StarModel>();
            _factory.MakeInstance(_starStat, SystemName, ref _star);
        }
        else {
            _star = _factory.MakeInstance(_starStat, _system);
        }
    }

    /// <summary>
    /// Makes the planets including assigning their Data component derived from the appropriate stat.
    /// </summary>
    private void MakePlanets() {
        LogEvent();
        if (isCompositionPreset) {
            _planets = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetModel>().ToList();
            if (_planets.Any()) {
                var planetsAlreadyUsed = new List<PlanetModel>();
                foreach (var planetStat in _planetStats) {  // there is a custom stat for each planet
                    // find a preExisting planet of the right category first to provide to Make
                    var planetsOfStatCategory = _planets.Where(p => p.category == planetStat.Category);
                    var planetsOfStatCategoryStillAvailable = planetsOfStatCategory.Except(planetsAlreadyUsed);
                    if (planetsOfStatCategoryStillAvailable.Any()) {    // IEnumerable.First() does not like empty IEnumerables
                        var planet = planetsOfStatCategoryStillAvailable.First();
                        planetsAlreadyUsed.Add(planet);
                        _factory.MakeInstance(planetStat, SystemName, ref planet);
                    }
                }
            }
        }
        else {
            _planets = new List<PlanetModel>(maxRandomPlanets);
            foreach (var planetStat in _planetStats) {
                var planet = _factory.MakeInstance(planetStat, _system);
                _planets.Add(planet);
            }
        }
    }

    /// <summary>
    /// Assigns and applies a CelestialOrbitSlot to each planet and adjusts its
    /// name to reflect that orbit. The position of the planet is automatically adjusted to fit within the slot
    /// when the orbit slot is assigned. Applies to both randomly-generated and preset planets.
    /// Also reserves an orbit slot for a future settlement.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void AssignSystemOrbitSlotsToPlanets() {
        LogEvent();
        int innerOrbitsCount = Mathf.FloorToInt(0.25F * TempGameValues.TotalOrbitSlotsPerSystem);
        int midOrbitsCount = Mathf.CeilToInt(0.6F * TempGameValues.TotalOrbitSlotsPerSystem) - innerOrbitsCount;
        int outerOrbitsCount = TempGameValues.TotalOrbitSlotsPerSystem - innerOrbitsCount - midOrbitsCount;

        var shuffledInnerStack = new Stack<int>(Enumerable.Range(0, innerOrbitsCount).Shuffle());
        var shuffledMidStack = new Stack<int>(Enumerable.Range(innerOrbitsCount, midOrbitsCount).Shuffle());
        var shuffledOuterStack = new Stack<int>(Enumerable.Range(innerOrbitsCount + midOrbitsCount, outerOrbitsCount).Shuffle());

        CelestialOrbitSlot[] allSystemOrbitSlots = GenerateAllSystemOrbitSlots(out _systemOrbitSlotDepth);

        // reserve a slot for a future Settlement
        int settlementOrbitSlotIndex = shuffledMidStack.Pop();
        _system.Data.SettlementOrbitSlot = allSystemOrbitSlots[settlementOrbitSlotIndex];

        // now divy up the remaining slots among the planets
        IList<PlanetModel> planetsToDestroy = null;
        Stack<int>[] slots;
        foreach (var planet in _planets) {
            var planetCategory = planet.Data.Category;
            switch (planetCategory) {
                case PlanetoidCategory.Volcanic:
                    slots = new Stack<int>[] { shuffledInnerStack, shuffledMidStack };
                    break;
                case PlanetoidCategory.Terrestrial:
                    slots = new Stack<int>[] { shuffledMidStack, shuffledInnerStack, shuffledOuterStack };
                    break;
                case PlanetoidCategory.Ice:
                    slots = new Stack<int>[] { shuffledOuterStack, shuffledMidStack };
                    break;
                case PlanetoidCategory.GasGiant:
                    slots = new Stack<int>[] { shuffledOuterStack, shuffledMidStack };
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

            int slotIndex = 0;
            if (TryFindOrbitSlot(out slotIndex, slots)) {
                CelestialOrbitSlot orbitSlotForPlanet = allSystemOrbitSlots[slotIndex];
                string name = SystemName + Constants.Space + _planetNumbers[slotIndex];
                string orbiterName = name + " Orbiter";
                orbitSlotForPlanet.AssumeOrbit(planet.transform, orbiterName);
                // assign the planet's name using its orbital slot
                planet.Data.Name = name;
                D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, slotIndex, SystemName);
            }
            else {
                if (planetsToDestroy == null) {
                    planetsToDestroy = new List<PlanetModel>();
                }
                planetsToDestroy.Add(planet);
            }
        }
        if (planetsToDestroy != null) {
            planetsToDestroy.ForAll(p => {
                _planets.Remove(p);
                D.Log("Destroying Planet {0}.", p.FullName);
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

    /// <summary>
    /// Makes the moons including assigning their Data component derived from the appropriate stat.
    /// </summary>
    private void MakeMoons() {
        LogEvent();
        if (isCompositionPreset) {
            _moons = gameObject.GetComponentsInChildren<MoonModel>().ToList();
            if (_moons.Any()) {
                var moonsAlreadyUsed = new List<MoonModel>();
                foreach (var planet in _planets) {
                    var moons = planet.gameObject.GetComponentsInChildren<MoonModel>();
                    if (moons.Any()) {
                        foreach (var moonStat in _moonStats) {  // there is a custom stat for each moon
                            // find a preExisting moon of the right category first to provide to Make
                            var moonsOfStatCategory = moons.Where(m => m.category == moonStat.Category);
                            var moonsOfStatCategoryStillAvailable = moonsOfStatCategory.Except(moonsAlreadyUsed);
                            if (moonsOfStatCategoryStillAvailable.Any()) {  // IEnumerable.First doesn't like empty IEnumerables
                                var moon = moonsOfStatCategoryStillAvailable.First();
                                moonsAlreadyUsed.Add(moon);
                                _factory.MakeInstance(moonStat, planet.Data.Name, ref moon);
                            }
                        }
                    }
                }
            }
        }
        else {
            _moons = new List<MoonModel>(maxRandomMoons);
            foreach (var moonStat in _moonStats) {
                var chosenPlanet = RandomExtended<PlanetModel>.Choice(_planets);
                var moon = _factory.MakeInstance(moonStat, chosenPlanet);
                _moons.Add(moon);
            }
        }
    }

    /// <summary>
    /// Assigns and applies a CelestialOrbitSlot around a planet to each moon. If the orbit slot proposed would potentially 
    /// interfere with other orbiting bodies or ships, the moon is destroyed. The position of the moon 
    /// is automatically adjusted to fit within the slot when the orbit slot is assigned. 
    /// Applies to both randomly-generated and preset moons.
    /// </summary>
    private void AssignPlanetOrbitSlotsToMoons() {
        LogEvent();
        IList<MoonModel> moonsToDestroy = null;
        foreach (var planet in _planets) {
            float depthAvailForMoonOrbitsAroundPlanet = _systemOrbitSlotDepth;

            float startDepthForMoonOrbitSlot = planet.ShipOrbitSlot.OuterRadius;
            IEnumerable<MoonModel> moons = planet.gameObject.GetComponentsInChildren<MoonModel>();
            if (moons.Any()) {
                int slotIndex = Constants.Zero;
                foreach (var moon in moons) {
                    float depthReqdForMoonOrbitSlot = 2F * moon.ShipOrbitSlot.OuterRadius;
                    float endDepthForMoonOrbitSlot = startDepthForMoonOrbitSlot + depthReqdForMoonOrbitSlot;
                    if (endDepthForMoonOrbitSlot <= depthAvailForMoonOrbitsAroundPlanet) {
                        string name = planet.Data.Name + _moonLetters[slotIndex];
                        moon.Data.Name = name;
                        GameTimeDuration orbitPeriod = _minMoonOrbitPeriod + (slotIndex * _moonOrbitPeriodIncrement);
                        var moonOrbitSlot = new CelestialOrbitSlot(startDepthForMoonOrbitSlot, endDepthForMoonOrbitSlot, planet, orbitPeriod);
                        string orbiterName = name + " Orbiter";
                        moonOrbitSlot.AssumeOrbit(moon.transform, orbiterName);
                        D.Log("{0} has assumed orbit slot {1} around Planet {2}.", moon.FullName, slotIndex, planet.FullName);

                        startDepthForMoonOrbitSlot = endDepthForMoonOrbitSlot;
                        slotIndex++;
                    }
                    else {
                        if (moonsToDestroy == null) {
                            moonsToDestroy = new List<MoonModel>();
                        }
                        D.Log("{0} scheduled for destruction. OrbitSlot outer depth {1} > available depth {2}.",
                            moon.FullName, endDepthForMoonOrbitSlot, depthAvailForMoonOrbitsAroundPlanet);
                        moonsToDestroy.Add(moon);
                    }
                }
            }
        }
        if (moonsToDestroy != null) {
            moonsToDestroy.ForAll(m => {
                _moons.Remove(m);
                D.Log("Destroying Moon {0}.", m.FullName);
                Destroy(m.gameObject);
            });
        }
    }

    private void AddMembersToSystemData() {
        LogEvent();
        _system.Data.StarData = _star.Data;
        _planets.Select(p => p.Data).ForAll(pd => _system.Data.AddPlanetoid(pd));
        _moons.Select(m => m.Data).ForAll(md => _system.Data.AddPlanetoid(md));
    }

    private void InitializeTopographyMonitor() {
        var monitor = gameObject.GetSafeMonoBehaviourComponentInChildren<TopographyMonitor>();
        monitor.Topography = SpaceTopography.System;
        monitor.SurroundingTopography = SpaceTopography.OpenSpace;
        monitor.TopographyRadius = TempGameValues.SystemRadius;
    }

    private void CompleteSystem() {
        LogEvent();
        // include the System as a target in any child with a CameraLOSChangedRelay
        _system.gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(r => r.AddTarget(_system.transform));
    }

    private void EnableSystem(Action onCompletion = null) {
        LogEvent();
        _planets.ForAll(p => p.enabled = true);
        _moons.ForAll(m => m.enabled = true);
        _system.enabled = true;
        _star.enabled = true;
        // Enable the Views of the Models 
        _planets.ForAll(p => p.gameObject.GetSafeMonoBehaviourComponent<AItemView>().enabled = true);
        _moons.ForAll(m => m.gameObject.GetSafeMonoBehaviourComponent<AItemView>().enabled = true);
        _system.gameObject.GetSafeMonoBehaviourComponent<AItemView>().enabled = true;
        _star.gameObject.GetSafeMonoBehaviourComponent<AItemView>().enabled = true;
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            if (onCompletion != null) {
                onCompletion();
            }
        });
    }

    /// <summary>
    /// Enables selected children of the system, star, planets and moons. e.g. - cameraLOSRelays
    /// Revolve and Orbits, etc. These scripts that are enabled should only be enabled on or after IsRunning.
    /// </summary>
    /// <param name="onCompletion">The on completion.</param>
    private void EnableOtherWhenRunning(Action onCompletion = null) {
        D.Assert(GameStatus.Instance.IsRunning);
        gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(relay => relay.enabled = true);

        // Enable planet and moon orbits. Leave any possible settlement that might already be present to the SettlementCreator
        _planets.ForAll(p => p.gameObject.GetComponentInParents<Orbiter>().enabled = true);   // planet orbits

        var allShipOrbiters = gameObject.GetComponentsInChildren<OrbiterForShips>();
        _planets.ForAll(p => p.gameObject.GetComponentsInChildren<Orbiter>().Except(allShipOrbiters).ForAll(o => o.enabled = true));  // moon orbits

        // OrbitersForShips are enabled and disabled by ShipOrbitSlot when ships assume and break orbit

        // Enable planet, moon and star revolves. Leave any possible settlement that might already be present to the SettlementCreator
        _planets.ForAll(p => p.gameObject.GetComponentsInChildren<Revolver>().ForAll(r => r.enabled = true));    // planets and moons
        _star.gameObject.GetComponentsInChildren<Revolver>().ForAll(r => r.enabled = true);

        gameObject.GetSafeMonoBehaviourComponentInChildren<TopographyMonitor>().enabled = true;
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            if (onCompletion != null) {
                onCompletion();
            }
        });
    }

    private void __SetIntelLevel() {    // UNCLEAR how should system, star, planet and moon intel coverage levels relate to each other?
        LogEvent();
        // Stars use FixedIntel set to Comprehensive. It is not changeable
        // Systems simply use Intel like most other objects. It can be changed to any value
        _system.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        // Planets and Moons use ImprovingIntel which means once a level is achieved it cannot be reduced
        _planets.ForAll(p => p.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive);
        _moons.ForAll(m => m.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive);
    }

    private void BeginSystemOperations(Action onCompletion) {
        LogEvent();
        _planets.ForAll(p => p.CommenceOperations());
        _moons.ForAll(m => m.CommenceOperations());
        UnityUtility.WaitOneToExecute(onWaitFinished: (wasKilled) => {
            onCompletion();
        });
    }

    private void DestroyCreationObject() {
        D.Assert(_transform.childCount == 1);
        foreach (Transform child in _transform) {
            child.parent = _systemsFolder;
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Generates an ordered array of all CelestialOrbitSlots in the system. The first slot (index = 0) is the closest to the star, just outside
    /// the star's ShipOrbitDistance
    /// Note: These are system orbit slots that can be occupied by planets and settlements.
    /// </summary>
    /// <returns></returns>
    private CelestialOrbitSlot[] GenerateAllSystemOrbitSlots(out float systemOrbitSlotDepth) {
        D.Assert(_star.Radius != Constants.ZeroF, "{0}.Radius has not yet been set.".Inject(_star.FullName));   // confirm the star's Awake() has run so Radius is valid
        float sysOrbitSlotsStartRadius = _star.ShipOrbitSlot.OuterRadius;
        float systemRadiusAvailableForAllOrbits = TempGameValues.SystemRadius - sysOrbitSlotsStartRadius;
        systemOrbitSlotDepth = systemRadiusAvailableForAllOrbits / (float)TempGameValues.TotalOrbitSlotsPerSystem;

        var allOrbitSlots = new CelestialOrbitSlot[TempGameValues.TotalOrbitSlotsPerSystem];
        for (int slotIndex = 0; slotIndex < TempGameValues.TotalOrbitSlotsPerSystem; slotIndex++) {
            float insideRadius = sysOrbitSlotsStartRadius + _systemOrbitSlotDepth * slotIndex;
            float outsideRadius = insideRadius + _systemOrbitSlotDepth;
            var orbitPeriod = _minSystemOrbitPeriod + (slotIndex * _systemOrbitPeriodIncrement);
            //D.Log("{0}'s orbit slot index {1} OrbitPeriod = {2}.", SystemName, slotIndex, orbitPeriod);
            allOrbitSlots[slotIndex] = new CelestialOrbitSlot(insideRadius, outsideRadius, _system, orbitPeriod);
        }
        return allOrbitSlots;
    }

    private void RegisterGameStateProgressionReadiness(bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, GameState.BuildAndDeploySystems, isReady));
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


