﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
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
///     <description>Planets: The planetary system (planet, orbits, moons) gameobject will always hold the name
/// of the planet type. It should not change. The name of the planet gameobject itself must already be set 
/// if the planet is already in the scene. Otherwise, planet names will be automatically constructed using the name of the
/// system and the orbital slot they end up being assigned (eg. Regulas 1).</description>
/// </item>
/// /// <item>
///     <description>Stars, Settlements and Moons: Likewise, names of Stars, Settlements and Moons
/// will automatically be assigned when initialized, always bearing the name of their parent. (eg. [SystemName] Star, 
/// [SystemName] City, [PlanetName]a indicating a moon </description>
/// </item>
///  </remarks>
/// </summary>
public class SystemCreator : AMonoBase, IDisposable {

    private static IList<SystemModel> _allSystems = new List<SystemModel>();
    public static IList<SystemModel> AllSystems { get { return _allSystems; } }

    private static int _numberOfOrbitSlots = TempGameValues.SystemOrbitSlots;

    private static int[] _planetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static string[] _moonLetters = new string[] { "a", "b", "c", "d", "e" };

    private static IEnumerable<PlanetoidCategory> _acceptablePlanetCategories = new PlanetoidCategory[] { 
        PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial, PlanetoidCategory.Volcanic 
    };

    public string SystemName { get { return _transform.name; } }    // the SystemCreator carries the name of the System

    private Vector3[] _orbitSlots;
    private Vector3 _settlementOrbitSlot;

    private StarStat _starStat;
    private IList<PlanetoidStat> _planetStats;
    private SystemModel _system;
    private StarModel _star;
    private IList<PlanetoidModel> _planets;
    private IEnumerable<PlanetoidModel> _moons;

    private Transform _systemsFolder;
    private bool _isPresetSystem;

    private IList<IDisposable> _subscribers;
    private SystemFactory _factory;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        _systemsFolder = _transform.parent;
        _factory = SystemFactory.Instance;
        _isPresetSystem = _transform.childCount > 0;
        GenerateOrbitSlotStartLocation();
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanging));
    }

    private void OnGameStateChanging(GameState newGameState) {
        GameState previousGameState = _gameMgr.CurrentState;
        if (previousGameState == GameState.BuildAndDeploySystems) {
            AllSystems.Add(_system);
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
            PrepareForOperations(onCompleted: delegate {
                __SetIntelLevel();
                RegisterGameStateProgressionReadiness(isReady: true);
            });
        }

        if (gameState == GameState.Running) {
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
    }

    private StarStat CreateStarStat() {
        LogEvent();
        StarCategory category = GetStarCategory();
        string starName = SystemName + Constants.Space + CommonTerms.Star;
        return new StarStat(starName, category, 100, new OpeYield(0F, 0F, 100F), new XYield(XResource.Special_3, 0.3F));
    }

    private StarCategory GetStarCategory() {
        LogEvent();
        if (_isPresetSystem) {
            Transform transformCarryingStarCategory = gameObject.GetSafeMonoBehaviourComponentInChildren<StarModel>().transform;
            return DeriveCategory<StarCategory>(transformCarryingStarCategory);
        }
        return Enums<StarCategory>.GetRandom(excludeDefault: true);
    }

    private IList<PlanetoidStat> CreatePlanetStats() {
        LogEvent();
        var planetStats = new List<PlanetoidStat>();
        if (_isPresetSystem) {
            IEnumerable<PlanetoidModel> allPlanetoids = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>();
            // exclude moons
            var planets = allPlanetoids.Where(p => p.gameObject.GetComponentInParents<PlanetoidModel>(excludeSelf: true) == null).ToList();
            foreach (var planet in planets) {
                Transform transformCarryingPlanetCategory = planet.transform.parent.parent;
                PlanetoidCategory pCategory = DeriveCategory<PlanetoidCategory>(transformCarryingPlanetCategory);
                string planetName = planet.gameObject.name; // if already in scene, the planet should already be named for its system and orbit
                PlanetoidStat stat = new PlanetoidStat(planetName, 1000000F, 10000F, pCategory, 25, new OpeYield(3.1F, 2F, 4.8F), new XYield(XResource.Special_1, 0.3F));
                planetStats.Add(stat);
            }
        }
        else {
            int orbitSlotsAvailableForPlanets = _numberOfOrbitSlots - 1;    // 1 reserved for a Settlement 
            int planetCount = RandomExtended<int>.Range(0, orbitSlotsAvailableForPlanets);
            for (int i = 0; i < planetCount; i++) {
                PlanetoidCategory pCategory = RandomExtended<PlanetoidCategory>.Choice(_acceptablePlanetCategories);
                string planetName = "{0}, rest deferred until orbit assigned.".Inject(pCategory.GetName());
                PlanetoidStat stat = new PlanetoidStat(planetName, 1000000F, 10000F, pCategory, 25, new OpeYield(3.1F, 2F, 4.8F), new XYield(XResource.Special_1, 0.3F));
                planetStats.Add(stat);
            }
        }
        return planetStats;
    }

    #endregion


    private void PrepareForOperations(Action onCompleted = null) {
        LogEvent();
        MakeSystem();   // stars and planets need a system parent when built
        MakeStar();     // makes the star a child of the system
        MakePlanets();  // makes each planet a child of the system
        AssignMembersToOrbitSlots();    // modifies planet names to reflect the assigned orbit
        PopulateMoonsWithData();        // makes moon names based on the moon's (modified) planet name
        InitializeSystemData();         // adds star and planet data to the system's data component
        CompleteSystem();               // misc final touchup
        EnableSystem(onCompletion: delegate {
            if (onCompleted != null) {
                onCompleted();
            }
        });
    }

    private void MakeSystem() {
        LogEvent();
        if (_isPresetSystem) {
            _system = gameObject.GetSafeMonoBehaviourComponentInChildren<SystemModel>();
            _factory.MakeSystemInstance(SystemName, ref _system);
        }
        else {
            _system = _factory.MakeSystemInstance(SystemName, gameObject);
        }
    }

    private void MakeStar() {
        LogEvent();
        if (_isPresetSystem) {
            _star = gameObject.GetSafeMonoBehaviourComponentInChildren<StarModel>();
            _factory.MakeInstance(_starStat, SystemName, ref _star);
        }
        else {
            _star = _factory.MakeInstance(_starStat, _system.gameObject);
        }
    }

    private void MakePlanets() {
        LogEvent();
        if (_isPresetSystem) {
            IEnumerable<PlanetoidModel> allPlanetoids = gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidModel>();
            // exclude moons
            _planets = allPlanetoids.Where(p => p.gameObject.GetComponentInParents<PlanetoidModel>(excludeSelf: true) == null).ToList();
            var planetsAlreadyUsed = new List<PlanetoidModel>();
            foreach (var planetStat in _planetStats) {
                // find a preExisting planet of the right category first to provide to Make
                var planetsOfStatCategory = _planets.Where(p => DeriveCategory<PlanetoidCategory>(p.transform.parent.parent) == (planetStat.Category));
                var planetsOfStatCategoryStillAvailable = planetsOfStatCategory.Except(planetsAlreadyUsed);
                var planet = planetsOfStatCategoryStillAvailable.First();
                planetsAlreadyUsed.Add(planet);
                _factory.MakeInstance(planetStat, SystemName, ref planet);
            }
        }
        else {
            _planets = new List<PlanetoidModel>();
            foreach (var planetStat in _planetStats) {
                var planet = _factory.MakeInstance(planetStat, _system.gameObject);
                _planets.Add(planet);
            }
        }
    }

    private void AssignMembersToOrbitSlots() {
        LogEvent();
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
                planet.Data.Name = SystemName + Constants.Space + _planetNumbers[slotIndex];
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

    private void PopulateMoonsWithData() {
        LogEvent();
        _moons = new List<PlanetoidModel>();
        foreach (var planet in _planets) {
            IEnumerable<PlanetoidModel> moons = planet.gameObject.GetComponentsInChildren<PlanetoidModel>().Except(planet);
            if (!moons.IsNullOrEmpty()) {
                int letterIndex = 0;
                foreach (var moon in moons) {
                    string planetName = planet.Data.Name;
                    string moonName = planetName + _moonLetters[letterIndex];
                    PlanetoidCategory moonCategory = DeriveCategory<PlanetoidCategory>(moon.transform);
                    PlanetoidStat stat = new PlanetoidStat(moonName, 1000F, 100000F, moonCategory, 5, new OpeYield(0.1F, 1F, 0.8F));

                    PlanetoidData data = new PlanetoidData(stat) {
                        ParentName = SystemName
                        // Owners are all initialized to TempGameValues.NoPlayer by AItemData
                        // CombatStrength is default(CombatStrength), aka all values zero'd out
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

    private void InitializeSystemData() {
        LogEvent();
        _system.Data.StarData = _star.Data;
        _planets.Select(p => p.Data).ForAll(pd => _system.Data.AddPlanet(pd));
        _system.Data.SettlementOrbitSlot = _settlementOrbitSlot;
    }

    private void EnableSystem(Action onCompletion = null) {
        LogEvent();
        _planets.ForAll(p => p.enabled = true);
        _moons.ForAll(m => m.enabled = true);
        _system.enabled = true;
        _star.enabled = true;
        // Enable the Views of the Models 
        _planets.ForAll(p => p.gameObject.GetSafeInterface<IViewable>().enabled = true);
        _moons.ForAll(m => m.gameObject.GetSafeInterface<IViewable>().enabled = true);
        _system.gameObject.GetSafeInterface<IViewable>().enabled = true;
        _star.gameObject.GetSafeInterface<IViewable>().enabled = true;
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            if (onCompletion != null) {
                onCompletion();
            }
        });
    }

    private void CompleteSystem() {
        LogEvent();
        // include the System as a target in any child with a CameraLOSChangedRelay
        _system.gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(r => r.AddTarget(_system.transform));
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
        _planets.ForAll(p => p.CurrentState = PlanetoidState.Idling);
        _moons.ForAll(m => m.CurrentState = PlanetoidState.Idling);
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

    private void GenerateOrbitSlotStartLocation() {
        LogEvent();
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
        return GameUtility.DeriveEnumFromName<T>(transformContainingCategoryName.name);
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

