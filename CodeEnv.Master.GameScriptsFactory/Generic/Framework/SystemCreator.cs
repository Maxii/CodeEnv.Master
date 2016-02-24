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
using System.Collections;
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
public class SystemCreator : AMonoBase {

    public static IList<SystemItem> AllSystems { get { return _systemLookupBySectorIndex.Values.ToList(); } }

    /// <summary>
    /// Returns true if the sector indicated by sectorIndex contains a System.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="system">The system if present in the sector.</param>
    /// <returns></returns>
    public static bool TryGetSystem(Index3D sectorIndex, out SystemItem system) {
        return _systemLookupBySectorIndex.TryGetValue(sectorIndex, out system);
    }

    public static IList<APlanetoidItem> AllPlanetoids { get { return _allPlanetoids; } }
    public static IList<PlanetItem> AllPlanets { get { return _allPlanets; } }
    public static IList<MoonItem> AllMoons { get { return _allMoons; } }

    public static IList<StarItem> AllStars { get { return _allStars; } }

    private static IDictionary<Index3D, SystemItem> _systemLookupBySectorIndex = new Dictionary<Index3D, SystemItem>();
    private static IList<APlanetoidItem> _allPlanetoids = new List<APlanetoidItem>();
    private static IList<PlanetItem> _allPlanets = new List<PlanetItem>();
    private static IList<MoonItem> _allMoons = new List<MoonItem>();
    private static IList<StarItem> _allStars = new List<StarItem>();

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

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;

    public bool isCompositionPreset;    // Has Editor
    public int maxPlanetsInRandomSystem = 3;
    public int maxMoonsInRandomSystem = 3;
    public int countermeasuresPerPlanetoid = 1;
    public bool enableTrackingLabel = false;

    public string SystemName { get { return transform.name; } }    // the SystemCreator carries the name of the System

    private float _systemOrbitSlotDepth;

    private StarStat _starStat;
    private IList<PlanetStat> _planetStats;
    private IList<PlanetoidStat> _moonStats;
    private IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;

    private SystemItem _system;
    private StarItem _star;
    private IList<PlanetItem> _planets;
    private IList<MoonItem> _moons;
    private Transform _systemsFolder;
    private SystemFactory _factory;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        _systemsFolder = transform.parent;
        _factory = SystemFactory.Instance;
        D.Assert(isCompositionPreset == transform.childCount > 0, "{0}.{1} Composition Preset flag is incorrect.".Inject(SystemName, GetType().Name));
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
            _minMoonOrbitPeriod = new GameTimeDuration(hours: 0, days: 30, years: 0);
        }
        if (_moonOrbitPeriodIncrement == default(GameTimeDuration)) {
            _moonOrbitPeriodIncrement = new GameTimeDuration(hours: 0, days: 10, years: 0);
        }
    }

    private void Subscribe() {
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
        SubscribeStaticallyOnce();
    }


    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    private void SubscribeStaticallyOnce() {
        if (!_isStaticallySubscribed) {
            //D.Log("{0} is subscribing statically to {1}.", GetType().Name, _gameMgr.GetType().Name);
            _gameMgr.sceneLoaded += SceneLoadedEventHandler;
            _isStaticallySubscribed = true;
        }
    }

    #region Event and Property Change Handlers

    private static void SceneLoadedEventHandler(object sender, EventArgs e) {
        CleanupStaticMembers();
    }

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        GameState gameState = _gameMgr.CurrentState;
        BuildDeployAndBeginSystemOperationsDuringStartup(gameState);
    }

    /// <summary>
    /// Removes the planetoid from the AllPlanetoids static collection when the planetoid dies.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private static void PlanetoidDeathEventHandler(object sender, EventArgs e) {
        APlanetoidItem planetoidItem = sender as APlanetoidItem;
        _allPlanetoids.Remove(planetoidItem);
        if (planetoidItem is PlanetItem) {
            _allPlanets.Remove(planetoidItem as PlanetItem);
        }
        else {
            _allMoons.Remove(planetoidItem as MoonItem);
        }
    }

    #endregion

    private void BuildDeployAndBeginSystemOperationsDuringStartup(GameState gameState) {
        if (gameState == GameState.BuildAndDeploySystems) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: false);
            CreateStats();
            PrepareForOperations();
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildAndDeploySystems, isReady: true);
            // System is now prepared to receive a Settlement when it deploys
        }
        if (gameState == GameState.Running) {
            InitializeTopographyMonitor();    // no need to do earlier as ships initialize their Topography during commence operations
            BeginSystemOperations(onCompletion: delegate {
                // wait to allow any cellestial objects using the IEnumerator StateMachine to enter their starting state
                DestroyCreationObject(); // destruction deferred so __UniverseInitializer can complete its work
            });
        }
    }

    #region Create Stats

    private void CreateStats() {
        if (isCompositionPreset) {
            _starStat = CreateStarStatFromChildren();
            _planetStats = CreatePlanetStatsFromChildren();
            _moonStats = CreateMoonStatsFromChildren();
        }
        else {
            _starStat = CreateRandomStarStat();
            _planetStats = CreateRandomPlanetStats();
            _moonStats = CreateRandomMoonStats();
        }
        _availablePassiveCountermeasureStats = __CreateAvailablePassiveCountermeasureStats(9);
    }

    private StarStat CreateStarStatFromChildren() {
        D.Assert(isCompositionPreset);
        StarCategory category = gameObject.GetSingleComponentInChildren<StarItem>().category;
        float radius = TempGameValues.StarRadius;
        float lowOrbitRadius = radius + 2F;
        int capacity = 100;
        return new StarStat(category, radius, lowOrbitRadius, capacity, CreateRandomResourceYield(ResourceCategory.Common, ResourceCategory.Strategic));
    }

    private StarStat CreateRandomStarStat() {
        D.Assert(!isCompositionPreset);
        StarCategory category = Enums<StarCategory>.GetRandom(excludeDefault: true);
        float radius = TempGameValues.StarRadius;
        float lowOrbitRadius = radius + 2F;
        int capacity = 100;
        return new StarStat(category, radius, lowOrbitRadius, capacity, CreateRandomResourceYield(ResourceCategory.Common, ResourceCategory.Strategic));
    }

    private IList<PlanetStat> CreatePlanetStatsFromChildren() {
        D.Assert(isCompositionPreset);
        var planetStats = new List<PlanetStat>();
        var planets = gameObject.GetSafeComponentsInChildren<PlanetItem>();
        foreach (var planet in planets) {
            PlanetoidCategory pCategory = planet.category;
            PlanetStat stat = __MakePlanetStat(pCategory);
            planetStats.Add(stat);
        }
        return planetStats;
    }

    private IList<PlanetStat> CreateRandomPlanetStats() {
        D.Assert(!isCompositionPreset);
        var planetStats = new List<PlanetStat>();
        int planetCount = maxPlanetsInRandomSystem;
        //D.Log("{0} random planet count = {1}.", SystemName, planetCount);
        for (int i = 0; i < planetCount; i++) {
            PlanetoidCategory pCategory = RandomExtended.Choice(_acceptablePlanetCategories);
            PlanetStat stat = __MakePlanetStat(pCategory);
            planetStats.Add(stat);
        }
        return planetStats;
    }

    private IList<PlanetoidStat> CreateMoonStatsFromChildren() {
        D.Assert(isCompositionPreset);
        var moonStats = new List<PlanetoidStat>();
        var moons = gameObject.GetComponentsInChildren<MoonItem>();
        foreach (var moon in moons) {
            var mCategory = moon.category;
            PlanetoidStat stat = __MakeMoonStat(mCategory);
            moonStats.Add(stat);
        }
        return moonStats;
    }

    private IList<PlanetoidStat> CreateRandomMoonStats() {
        D.Assert(!isCompositionPreset);
        var moonStats = new List<PlanetoidStat>();
        int moonCount = maxMoonsInRandomSystem;
        //D.Log("{0} random moon count = {1}.", SystemName, moonCount);
        for (int i = 0; i < moonCount; i++) {
            PlanetoidCategory mCategory = RandomExtended.Choice(_acceptableMoonCategories);
            PlanetoidStat stat = __MakeMoonStat(mCategory);
            moonStats.Add(stat);
        }
        return moonStats;
    }

    private IList<PassiveCountermeasureStat> __CreateAvailablePassiveCountermeasureStats(int quantity) {
        IList<PassiveCountermeasureStat> statsList = new List<PassiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            DamageStrength damageMitigation;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
            float damageMitigationValue;
            switch (damageMitigationCategory) {
                case DamageCategory.Thermal:
                    name = "HighVaporAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Atomic:
                    name = "HighAcidAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Kinetic:
                    name = "HighParticulateAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.None:
                    name = "NoAtmosphere";
                    damageMitigation = new DamageStrength(1F, 1F, 1F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
            }
            var countermeasureStat = new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    #endregion

    #region PrepareForOperations

    private void PrepareForOperations() {
        LogEvent();
        MakeSystem();   // stars and planets need a system parent when built
        MakeStar();     // makes the star a child of the system
        MakePlanets();  // makes each planet a child of the system
        AssignSystemOrbitSlotsToPlanets();    // modifies planet names to reflect the assigned orbit
        MakeMoons();    // makes each moon a child of a planet
        AssignPlanetOrbitSlotsToMoons();    // modifies moon names based on its assigned planetary orbit
        AddMembersToSystem();
        RecordInStaticCollections();
    }

    private void MakeSystem() {
        LogEvent();
        CameraFocusableStat cameraStat = __MakeSystemCameraStat();
        if (isCompositionPreset) {
            _system = gameObject.GetSingleComponentInChildren<SystemItem>();
            _factory.PopulateSystemInstance(SystemName, cameraStat, ref _system);
        }
        else {
            _system = _factory.MakeSystemInstance(SystemName, gameObject, cameraStat);
        }
        _system.IsTrackingLabelEnabled = enableTrackingLabel;
        SectorGrid.Instance.GetSector(_system.SectorIndex).System = _system;
    }

    private void MakeStar() {
        LogEvent();
        CameraFocusableStat cameraStat = __MakeStarCameraStat(_starStat.Radius, _starStat.LowOrbitRadius);
        if (isCompositionPreset) {
            _star = gameObject.GetSingleComponentInChildren<StarItem>();
            _factory.PopulateInstance(_starStat, cameraStat, SystemName, ref _star);
        }
        else {
            _star = _factory.MakeInstance(_starStat, cameraStat, _system.gameObject, SystemName);
        }
    }

    /// <summary>
    /// Makes the planets including assigning their Data component derived from the appropriate stat.
    /// </summary>
    private void MakePlanets() {
        LogEvent();
        if (isCompositionPreset) {
            _planets = gameObject.GetSafeComponentsInChildren<PlanetItem>().ToList();
            if (_planets.Any()) {
                var planetsAlreadyUsed = new List<PlanetItem>();
                foreach (var planetStat in _planetStats) {  // there is a custom stat for each planet
                    // find a preExisting planet of the right category first to provide to Make
                    var planetsOfStatCategory = _planets.Where(p => p.category == planetStat.Category);
                    var planetsOfStatCategoryStillAvailable = planetsOfStatCategory.Except(planetsAlreadyUsed);
                    if (planetsOfStatCategoryStillAvailable.Any()) {    // IEnumerable.First() does not like empty IEnumerables
                        var planet = planetsOfStatCategoryStillAvailable.First();
                        var countermeasureStats = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerPlanetoid);
                        CameraFollowableStat cameraStat = __MakePlanetCameraStat(planetStat);
                        planetsAlreadyUsed.Add(planet);
                        _factory.PopulateInstance(planetStat, cameraStat, countermeasureStats, SystemName, ref planet);
                    }
                }
            }
        }
        else {
            _planets = new List<PlanetItem>(maxPlanetsInRandomSystem);
            foreach (var planetStat in _planetStats) {
                var countermeasureStats = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerPlanetoid);
                CameraFollowableStat cameraStat = __MakePlanetCameraStat(planetStat);
                var planet = _factory.MakeInstance(planetStat, cameraStat, countermeasureStats, _system);
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
        IList<PlanetItem> planetsToDestroy = null;
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
                string orbitSimulatorName = name + " OrbitSimulator";
                orbitSlotForPlanet.AssumeOrbit(planet.transform, orbitSimulatorName);
                // assign the planet's name using its orbital slot
                planet.Data.Name = name;
                D.Warn(planet.ShipOrbitSlot.OuterRadius > _systemOrbitSlotDepth, "{0}: {1} reqd orbit slot depth of {2:0.#} > SystemOrbitSlotDepth of {3:0.#}."
                    , GetType().Name, planet.FullName, planet.ShipOrbitSlot.OuterRadius, _systemOrbitSlotDepth);
                //D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, slotIndex, SystemName);
            }
            else {
                if (planetsToDestroy == null) {
                    planetsToDestroy = new List<PlanetItem>();
                }
                planetsToDestroy.Add(planet);
            }
        }
        if (planetsToDestroy != null) {
            planetsToDestroy.ForAll(p => {
                _planets.Remove(p);
                //D.Log("Destroying Planet {0}.", p.FullName);
                Destroy(p.gameObject);
            });
        }
    }

    private bool TryFindOrbitSlot(out int slotIndex, params Stack<int>[] slotStacks) {
        foreach (var slotStack in slotStacks) {
            if (slotStack.Count > 0) {
                slotIndex = slotStack.Pop();
                return true;
            }
        }
        slotIndex = -1;
        return false;
    }

    /// <summary>
    /// Makes the moons including assigning their Data component derived from the appropriate stat.
    /// Each moon is parented to a random planet if not already preset.
    /// </summary>
    private void MakeMoons() {
        LogEvent();
        if (isCompositionPreset) {
            _moons = gameObject.GetComponentsInChildren<MoonItem>().ToList();
            if (_moons.Any()) {
                var moonsAlreadyUsed = new List<MoonItem>();
                foreach (var planet in _planets) {
                    var moons = planet.gameObject.GetComponentsInChildren<MoonItem>();
                    if (moons.Any()) {
                        foreach (var moonStat in _moonStats) {  // there is a custom stat for each moon
                            // find a preExisting moon of the right category first to provide to Make
                            var moonsOfStatCategory = moons.Where(m => m.category == moonStat.Category);
                            var moonsOfStatCategoryStillAvailable = moonsOfStatCategory.Except(moonsAlreadyUsed);
                            if (moonsOfStatCategoryStillAvailable.Any()) {  // IEnumerable.First doesn't like empty IEnumerables
                                var moon = moonsOfStatCategoryStillAvailable.First();
                                moonsAlreadyUsed.Add(moon);
                                var countermeasureStats = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerPlanetoid);
                                CameraFollowableStat cameraStat = __MakeMoonCameraStat(moonStat);
                                _factory.PopulateInstance(moonStat, cameraStat, countermeasureStats, planet.Data.Name, ref moon);
                            }
                        }
                    }
                }
            }
        }
        else {
            _moons = new List<MoonItem>(maxMoonsInRandomSystem);
            foreach (var moonStat in _moonStats) {
                var chosenPlanet = RandomExtended.Choice(_planets);
                var countermeasureStats = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerPlanetoid);
                CameraFollowableStat cameraStat = __MakeMoonCameraStat(moonStat);
                var moon = _factory.MakeInstance(moonStat, cameraStat, countermeasureStats, chosenPlanet);
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
        IList<MoonItem> moonsToDestroy = null;
        foreach (var planet in _planets) {
            float depthAvailForMoonOrbitsAroundPlanet = _systemOrbitSlotDepth;

            float startDepthForMoonOrbitSlot = planet.ShipOrbitSlot.OuterRadius;
            var moons = planet.GetComponentsInChildren<MoonItem>();
            if (moons.Any()) {
                int slotIndex = Constants.Zero;
                foreach (var moon in moons) {
                    float depthReqdForMoonOrbitSlot = 2F * moon.ObstacleZoneRadius;
                    float endDepthForMoonOrbitSlot = startDepthForMoonOrbitSlot + depthReqdForMoonOrbitSlot;
                    if (endDepthForMoonOrbitSlot <= depthAvailForMoonOrbitsAroundPlanet) {
                        string name = planet.Data.Name + _moonLetters[slotIndex];
                        moon.Data.Name = name;
                        GameTimeDuration orbitPeriod = _minMoonOrbitPeriod + (slotIndex * _moonOrbitPeriodIncrement);
                        var moonOrbitSlot = new CelestialOrbitSlot(startDepthForMoonOrbitSlot, endDepthForMoonOrbitSlot, planet.gameObject, true, orbitPeriod);
                        string orbitSimulatorName = name + " OrbitSimulator";
                        moonOrbitSlot.AssumeOrbit(moon.transform, orbitSimulatorName);
                        //D.Log("{0} has assumed orbit slot {1} around Planet {2}.", moon.FullName, slotIndex, planet.FullName);

                        startDepthForMoonOrbitSlot = endDepthForMoonOrbitSlot;
                        slotIndex++;
                    }
                    else {
                        if (moonsToDestroy == null) {
                            moonsToDestroy = new List<MoonItem>();
                        }
                        D.Warn(slotIndex == Constants.Zero, "{0}: Size of planet {1} precludes adding any moons.", GetType().Name, planet.FullName);
                        //D.Log("{0} scheduled for destruction. OrbitSlot outer depth {1} > available depth {2}.",
                        //    moon.FullName, endDepthForMoonOrbitSlot, depthAvailForMoonOrbitsAroundPlanet);
                        moonsToDestroy.Add(moon);
                    }
                }
            }
        }
        if (moonsToDestroy != null) {
            moonsToDestroy.ForAll(m => {
                _moons.Remove(m);
                //D.Log("Destroying Moon {0}.", m.FullName);
                Destroy(m.gameObject);
            });
        }
    }

    private void AddMembersToSystem() {
        LogEvent();
        _system.Star = _star;
        _planets.ForAll(p => _system.AddPlanetoid(p));
        _moons.ForAll(m => _system.AddPlanetoid(m));
    }

    private void InitializeTopographyMonitor() {
        var monitor = gameObject.GetSingleComponentInChildren<TopographyMonitor>();
        monitor.SurroundingTopography = Topography.OpenSpace;
        monitor.ParentItem = _system;
    }

    /// <summary>
    /// Records the System, Star and Planetoids in the appropriate static collection holding all instances.
    /// Note: The Assert tests are here to make sure instances from a prior scene are not still present, as the collections
    /// these items are stored in are static and persist across scenes.
    /// </summary>
    private void RecordInStaticCollections() {
        var key = _system.Data.SectorIndex;
        D.Assert(!_systemLookupBySectorIndex.ContainsKey(key), "{0}.{1} reports Key {2} already present.".Inject(SystemName, GetType().Name, key));
        _systemLookupBySectorIndex.Add(key, _system);

        // Can't use a Contains(item) test as the new item instance will never equal the old instance from the previous scene, even with the same name
        var starNamesStored = _allStars.Select(s => s.Name);
        D.Assert(!starNamesStored.Contains(_star.Name), "{0}.{1} reports {2} already present.".Inject(SystemName, GetType().Name, _star.Name));
        _allStars.Add(_star);

        // Can't use a Contains(item) test as the new item instance will never equal the old instance from the previous scene, even with the same name
        var planetoidNamesStored = _allPlanetoids.Select(p => p.Name);
        _planets.ForAll(planet => {
            D.Assert(!planetoidNamesStored.Contains(planet.Name), "{0}.{1} reports {2} already present.".Inject(SystemName, GetType().Name, planet.Name));
            planet.deathOneShot += PlanetoidDeathEventHandler;
            _allPlanetoids.Add(planet);
            _allPlanets.Add(planet);
        });

        // Can't use a Contains(item) test as the new item instance will never equal the old instance from the previous scene, even with the same name
        _moons.ForAll(moon => {
            D.Assert(!planetoidNamesStored.Contains(moon.Name), "{0}.{1} reports {2} already present.".Inject(SystemName, GetType().Name, moon.Name));
            moon.deathOneShot += PlanetoidDeathEventHandler;
            _allPlanetoids.Add(moon);
            _allMoons.Add(moon);
        });
    }

    #endregion

    private void BeginSystemOperations(Action onCompletion) {
        LogEvent();
        _planets.ForAll(p => p.CommenceOperations());
        _moons.ForAll(m => m.CommenceOperations());
        _star.CommenceOperations();
        _system.CommenceOperations();
        UnityUtility.WaitOneToExecute(onWaitFinished: () => {
            onCompletion();
        });
    }

    private void DestroyCreationObject() {
        D.Assert(transform.childCount == 1);
        foreach (Transform child in transform) {
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
        //D.Log("{0}: SystemOrbitSlotDepth = {1:0.#}.", SystemName, systemOrbitSlotDepth);

        var allOrbitSlots = new CelestialOrbitSlot[TempGameValues.TotalOrbitSlotsPerSystem];
        for (int slotIndex = 0; slotIndex < TempGameValues.TotalOrbitSlotsPerSystem; slotIndex++) {
            float insideRadius = sysOrbitSlotsStartRadius + systemOrbitSlotDepth * slotIndex;
            float outsideRadius = insideRadius + systemOrbitSlotDepth;
            var orbitPeriod = _minSystemOrbitPeriod + (slotIndex * _systemOrbitPeriodIncrement);
            //D.Log("{0}'s orbit slot index {1} OrbitPeriod = {2}.", SystemName, slotIndex, orbitPeriod);
            GameObject planetsFolder = _system.transform.FindChild("Planets").gameObject;
            // planetsFolder used in place of _system so orbiters don't inherit the layer of the system
            D.Assert(planetsFolder != null);    // in case I accidently change name of PlanetsFolder
            allOrbitSlots[slotIndex] = new CelestialOrbitSlot(insideRadius, outsideRadius, planetsFolder, _system.IsMobile, orbitPeriod);
        }
        return allOrbitSlots;
    }

    private ResourceYield CreateRandomResourceYield(params ResourceCategory[] resCategories) {
        ResourceYield sum = default(ResourceYield);
        resCategories.ForAll(resCat => sum += CreateRandomResourceYield(resCat));
        return sum;
    }

    private ResourceYield CreateRandomResourceYield(ResourceCategory resCategory) {
        float maxYield = Constants.OneF;
        int minNumberOfResources = Constants.Zero;
        switch (resCategory) {
            case ResourceCategory.Common:
                minNumberOfResources = Constants.One;
                maxYield = 5F;
                break;
            case ResourceCategory.Strategic:
                maxYield = 2F;
                break;
            case ResourceCategory.Luxury:   // No Luxury Resources yet
            case ResourceCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resCategory));
        }

        var categoryResources = Enums<ResourceID>.GetValues(excludeDefault: true).Where(res => res.GetResourceCategory() == resCategory);
        int categoryResourceCount = categoryResources.Count();
        int numberOfResourcesToCreate = RandomExtended.Range(minNumberOfResources, categoryResourceCount);

        IList<ResourceYield.ResourceValuePair> resValuePairs = new List<ResourceYield.ResourceValuePair>(numberOfResourcesToCreate);
        var resourcesChosen = categoryResources.Shuffle().Take(numberOfResourcesToCreate);
        resourcesChosen.ForAll(resID => {
            var rvp = new ResourceYield.ResourceValuePair(resID, UnityEngine.Random.Range(Constants.ZeroF, maxYield));
            resValuePairs.Add(rvp);
        });
        return new ResourceYield(resValuePairs.ToArray());
    }

    private PlanetStat __MakePlanetStat(PlanetoidCategory pCategory) {
        float radius = __GetRadius(pCategory);
        float lowOrbitRadius = radius + 1F;
        return new PlanetStat(radius, 1000000F, 100F, pCategory, 25, CreateRandomResourceYield(ResourceCategory.Common, ResourceCategory.Strategic), lowOrbitRadius);
    }

    private PlanetoidStat __MakeMoonStat(PlanetoidCategory mCategory) {
        float radius = __GetRadius(mCategory);
        return new PlanetoidStat(radius, 10000F, 10F, mCategory, 5, CreateRandomResourceYield(ResourceCategory.Common));
    }

    private CameraFocusableStat __MakeSystemCameraStat() {
        float minViewDistance = 2F;   // 2 units from the orbital plane
        float optViewDistance = TempGameValues.SystemRadius;
        return new CameraFocusableStat(minViewDistance, optViewDistance, fov: 70F);
    }

    private CameraFocusableStat __MakeStarCameraStat(float radius, float lowOrbitRadius) {
        float minViewDistance = radius + 1F;
        float highOrbitRadius = lowOrbitRadius + TempGameValues.ShipOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new CameraFocusableStat(minViewDistance, optViewDistance, fov: 70F);
    }

    private CameraFollowableStat __MakePlanetCameraStat(PlanetStat planetStat) {
        float fov;
        PlanetoidCategory pCat = planetStat.Category;
        switch (pCat) {
            case PlanetoidCategory.GasGiant:
                fov = 70F;
                break;
            case PlanetoidCategory.Ice:
            case PlanetoidCategory.Terrestrial:
                fov = 65F;
                break;
            case PlanetoidCategory.Volcanic:
                fov = 60F;
                break;
            case PlanetoidCategory.Moon_005:
            case PlanetoidCategory.Moon_001:
            case PlanetoidCategory.Moon_002:
            case PlanetoidCategory.Moon_003:
            case PlanetoidCategory.Moon_004:
            case PlanetoidCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pCat));
        }
        float radius = planetStat.Radius;
        float minViewDistance = radius + 1F;
        float highOrbitRadius = planetStat.LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new CameraFollowableStat(minViewDistance, optViewDistance, fov);
    }

    private CameraFollowableStat __MakeMoonCameraStat(PlanetoidStat moonStat) {
        float fov;
        PlanetoidCategory pCat = moonStat.Category;
        switch (pCat) {
            case PlanetoidCategory.Moon_005:
                fov = 60F;
                break;
            case PlanetoidCategory.Moon_001:
            case PlanetoidCategory.Moon_002:
            case PlanetoidCategory.Moon_003:
            case PlanetoidCategory.Moon_004:
                fov = 50F;
                break;
            case PlanetoidCategory.GasGiant:
            case PlanetoidCategory.Ice:
            case PlanetoidCategory.Terrestrial:
            case PlanetoidCategory.Volcanic:
            case PlanetoidCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pCat));
        }
        float radius = moonStat.Radius;
        float minViewDistance = radius + 1F;
        float optViewDistance = radius + 3F;    // optViewDistance has no linkage to ObstacleZoneRadius
        return new CameraFollowableStat(minViewDistance, optViewDistance, fov);
    }

    private float __GetRadius(PlanetoidCategory cat) {
        switch (cat) {
            case PlanetoidCategory.GasGiant:
                return 5F;
            case PlanetoidCategory.Ice:
                return 2F;
            case PlanetoidCategory.Moon_001:
                return 0.2F;
            case PlanetoidCategory.Moon_002:
                return 0.2F;
            case PlanetoidCategory.Moon_003:
                return 0.2F;
            case PlanetoidCategory.Moon_004:
                return 0.5F;
            case PlanetoidCategory.Moon_005:
                return 1F;
            case PlanetoidCategory.Terrestrial:
                return 2F;
            case PlanetoidCategory.Volcanic:
                return 1F;
            case PlanetoidCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
        if (IsApplicationQuiting) {
            CleanupStaticMembers();
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    private void Unsubscribe() {
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
    }

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes or after quiting.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    private static void CleanupStaticMembers() {
        _systemLookupBySectorIndex.Clear();
        _allStars.Clear();
        _allPlanetoids.ForAll(p => p.deathOneShot -= PlanetoidDeathEventHandler);
        _allPlanetoids.Clear();
        _allPlanets.Clear();
        _allMoons.Clear();
    }

    /// <summary>
    /// Unsubscribes this class from all events that use a static event handler on Quit.
    /// </summary>
    private void UnsubscribeStaticallyOnceOnQuit() {
        if (_isStaticallySubscribed) {
            _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
            _isStaticallySubscribed = false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


