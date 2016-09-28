// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreator2.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// COMMENT 
/// </summary>
public class SystemCreator2 : AMonoBase {

    [Obsolete]
    protected const string PlanetNameFormat = "{0} {1}";

    private const string NameFormat = "{0}.{1}";
    //private const string UnnamedSystemName = "SystemCreator";
    //protected const int MaxPlanetsPerSystem = TempGameValues.TotalOrbitSlotsPerSystem - 1;
    //protected const int MaxMoonsPerSystem = MaxPlanetsPerSystem * 2;

    [Obsolete]
    private static int[] _planetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    [Obsolete]
    private static string[] _moonLetters = new string[] { "a", "b", "c", "d", "e" };

    // these must be set from Awake() 
    [Obsolete]
    private static GameTimeDuration _minSystemOrbitPeriod;
    [Obsolete]
    private static GameTimeDuration _systemOrbitPeriodIncrement;
    [Obsolete]
    private static GameTimeDuration _minMoonOrbitPeriod;
    [Obsolete]
    private static GameTimeDuration _moonOrbitPeriodIncrement;


    public string Name { get { return NameFormat.Inject(SystemName, GetType().Name); } }

    public string SystemName {
        get { return transform.name; }
        private set { transform.name = value; }
    }

    //public bool IsSystemNamed { get { return Utility.CheckForContent(SystemName); } }
    //public bool IsSystemNamed { get { return SystemName != UnnamedSystemName; } }

    public IntVector3 SectorIndex { get { return SectorGrid.Instance.GetSectorIndexThatContains(transform.position); } }


    private SystemCreatorConfiguration _configuration;
    public SystemCreatorConfiguration Configuration {
        get { return _configuration; }
        set { SetProperty<SystemCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationChangedHandler); }
    }

    //protected float _systemOrbitSlotDepth;
    protected SystemItem _system;
    protected StarItem _star;
    protected IList<PlanetItem> _planets;
    protected IList<MoonItem> _moons;
    //protected IDictionary<PlanetDesign, PlanetItem> _planetsLookup;
    //protected IDictionary<MoonDesign, MoonItem> _moonsLookup;


    //protected JobManager _jobMgr;
    protected GameManager _gameMgr;
    protected SystemFactory _systemFactory;
    //protected GeneralFactory _generalFactory;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        //SetStaticValues();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        //_jobMgr = JobManager.Instance;
        _gameMgr = GameManager.Instance;
        _systemFactory = SystemFactory.Instance;
        //_generalFactory = GeneralFactory.Instance;
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
    [Obsolete]
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
    }

    #region Event and Property Change Handlers

    protected override void Start() {
        base.Start();
        D.Assert(gameObject.isStatic, "{0} should be static after being positioned.", GetType().Name);
    }

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        GameState gameState = _gameMgr.CurrentState;
        BuildDeployAndBeginSystemOperationsDuringStartup(gameState);
    }

    #endregion

    private void BuildDeployAndBeginSystemOperationsDuringStartup(GameState gameState) {
        LogEvent();
        if (gameState == GameState.BuildingSystems) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildingSystems, isReady: false);
            BuildAndDeploySystem();
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildingSystems, isReady: true);
            // System is now prepared to receive a Settlement when it deploys
        }
        if (gameState == GameState.PreparingToRun) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PreparingToRun, isReady: false);
            CompleteSystemInitialization();
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PreparingToRun, isReady: true);
        }
        if (gameState == GameState.Running) {
            InitializeTopographyMonitor();    // no need to do earlier as ships initialize their Topography during commence operations
            BeginSystemOperations();
            DestroyCreationObject();    // 3.25.16 1 frame delay before destruction removed. See BeginSystemOperations
        }
    }

    private void ConfigurationChangedHandler() {
        SystemName = Configuration.SystemName;
    }


    private bool ValidateConfiguration() {
        if (Configuration == default(SystemCreatorConfiguration)) {
            D.Warn("{0} found Configuration unassigned so destroying system before created.", Name);
            Destroy(gameObject);
            return false;
        }
        return true;
    }

    #region Build and Deploy System

    private void BuildAndDeploySystem() {
        LogEvent();
        MakeSystem();   // stars and planets need a system parent when built
        MakeStar();     // makes the star a child of the system
        //_systemOrbitSlotDepth = __CalcSystemOrbitSlotDepth(_star);
        //MakePlanets();  // makes each planet a child of the system
        MakePlanetsAndPlaceInOrbits();
        //AssignSystemOrbitSlotsToPlanets();    // modifies planet names to reflect the assigned orbit
        //MakeMoons();    // makes each moon a child of a planet
        MakeMoonsAndPlaceInOrbits();
        //AssignPlanetOrbitSlotsToMoons();    // modifies moon names based on its assigned planetary orbit
        AddMembersToSystem();
        AddToGameKnowledge();
    }

    protected virtual void MakeSystem() {
        LogEvent();

        FocusableItemCameraStat cameraStat = MakeSystemCameraStat();
        _system = _systemFactory.MakeSystemInstance(SystemName, gameObject, cameraStat);
        D.Assert(_system.gameObject.isStatic, "{0} should be static after being positioned.", _system.FullName);

        _system.SettlementOrbitData = Configuration.SettlementOrbitSlot;
        _system.IsTrackingLabelEnabled = Configuration.IsTrackingLabelEnabled;
        SectorGrid.Instance.GetSector(_system.SectorIndex).System = _system;
    }
    //protected virtual void MakeSystem() {
    //    LogEvent();

    //    FocusableItemCameraStat cameraStat = MakeSystemCameraStat();
    //    _system = _systemFactory.MakeSystemInstance(SystemName, gameObject, cameraStat);
    //    D.Assert(_system.gameObject.isStatic, "{0} should be static after being positioned.", _system.FullName);

    //    _system.IsTrackingLabelEnabled = Configuration.IsTrackingLabelEnabled;
    //    SectorGrid.Instance.GetSector(_system.SectorIndex).System = _system;
    //}

    protected virtual void MakeStar() {
        LogEvent();
        StarDesign design = _gameMgr.CelestialDesigns.GetStarDesign(Configuration.StarDesignName);
        FocusableItemCameraStat cameraStat = MakeStarCameraStat(design.StarStat);
        _star = _systemFactory.MakeInstance(design, cameraStat, _system.gameObject, SystemName);
    }

    /// <summary>
    /// Makes the planets including assigning their Data component derived from the appropriate stat.
    /// </summary>
    //protected virtual void MakePlanets() {
    //    LogEvent();
    //    int planetQty = Configuration.PlanetDesignNames.Count;
    //    _planetsLookup = new Dictionary<PlanetDesign, PlanetItem>(planetQty);
    //    foreach (var designName in Configuration.PlanetDesignNames) {
    //        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
    //        FollowableItemCameraStat cameraStat = MakePlanetCameraStat(design.Stat);
    //        var planet = _systemFactory.MakeInstance(design, cameraStat, _system);
    //        _planetsLookup.Add(design, planet);
    //    }
    //}
    //protected virtual void MakePlanets() {
    //    LogEvent();
    //    int planetQty = Configuration.PlanetDesignNames.Count;
    //    _planets = new List<PlanetItem>(planetQty);
    //    foreach (var designName in Configuration.PlanetDesignNames) {
    //        PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
    //        FollowableItemCameraStat cameraStat = MakePlanetCameraStat(design.Stat);
    //        var planet = _systemFactory.MakeInstance(design, cameraStat, _system);
    //        _planets.Add(planet);
    //    }
    //}

    private void MakePlanetsAndPlaceInOrbits() {
        LogEvent();
        int planetQty = Configuration.PlanetDesignNames.Count;
        _planets = new List<PlanetItem>(planetQty);
        for (int index = 0; index < planetQty; index++) {
            string designName = Configuration.PlanetDesignNames[index];
            PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
            FollowableItemCameraStat cameraStat = MakePlanetCameraStat(design.Stat);
            OrbitData orbitSlot = Configuration.PlanetOrbitSlots[index];
            orbitSlot.OrbitedItem = _system.gameObject; // UNCLEAR orbit star?
            var planet = _systemFactory.MakeInstance(design, cameraStat, orbitSlot);
            float sysOrbitSlotDepth;
            D.Warn(planet.Data.CloseOrbitOuterRadius > (sysOrbitSlotDepth = __CalcSystemOrbitSlotDepth(_star)), "{0}: {1} reqd orbit slot depth of {2:0.#} > SystemOrbitSlotDepth of {3:0.#}.",
                Name, planet.FullName, planet.Data.CloseOrbitOuterRadius, sysOrbitSlotDepth);
            D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, orbitSlot.SlotIndex, SystemName);
            _planets.Add(planet);
        }
    }

    //private void AssignSystemOrbitSlotsToPlanets() {
    //    LogEvent();
    //    var allPlanetDesigns = _planetsLookup.Keys;
    //    var allSystemOrbitSlots = allPlanetDesigns.Select(design => design.OrbitSlot).Union(new OrbitData[] { Configuration.SettlementOrbitSlot });
    //    var ascendingSystemOrbitSlots = allSystemOrbitSlots.OrderBy(slot => slot.OuterRadius).ToList();  // from star to system edge
    //    int slotCount = ascendingSystemOrbitSlots.Count;

    //    IDictionary<OrbitData, PlanetDesign> planetDesignLookup = allPlanetDesigns.ToDictionary(design => design.OrbitSlot);
    //    for (int slotIndex = 0; slotIndex < slotCount; slotIndex++) {
    //        OrbitData slot = ascendingSystemOrbitSlots[slotIndex];
    //        if (slot == Configuration.SettlementOrbitSlot) {
    //            _system.SettlementOrbitData = slot;
    //            continue;
    //        }
    //        PlanetDesign design = planetDesignLookup[slot];
    //        PlanetItem planet = _planetsLookup[design];
    //        planet.Name = PlanetNameFormat.Inject(SystemName, _planetNumbers[slotIndex]);
    //        _generalFactory.InstallCelestialItemInOrbit(planet.gameObject, slot);
    //        D.Warn(planet.Data.CloseOrbitOuterRadius > _systemOrbitSlotDepth, "{0}: {1} reqd orbit slot depth of {2:0.#} > SystemOrbitSlotDepth of {3:0.#}."
    //            , GetType().Name, planet.FullName, planet.Data.CloseOrbitOuterRadius, _systemOrbitSlotDepth);
    //        D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, slotIndex, SystemName);
    //    }
    //}



    /// <summary>
    /// Makes the moons including assigning their Data component derived from the appropriate stat.
    /// Each moon is parented to a random planet if not already preset.
    /// </summary>
    //protected virtual void MakeMoons() {
    //    LogEvent();
    //    int moonQty = Configuration.MoonDesignNames.Count;
    //    _moonsLookup = new Dictionary<MoonDesign, MoonItem>(moonQty);
    //    foreach (var designName in Configuration.MoonDesignNames) {
    //        MoonDesign design = _gameMgr.CelestialDesigns.GetMoonDesign(designName);
    //        PlanetItem parentPlanet;
    //        if (__TryChooseParentPlanet(design.Stat, out parentPlanet)) {
    //            FollowableItemCameraStat cameraStat = MakeMoonCameraStat(design.Stat);
    //            var moon = _systemFactory.MakeInstance(design, cameraStat, parentPlanet);
    //            _moonsLookup.Add(design, moon);
    //        }
    //    }
    //}

    private void MakeMoonsAndPlaceInOrbits() {
        LogEvent();
        _moons = new List<MoonItem>();
        int planetQty = _planets.Count;
        for (int planetIndex = 0; planetIndex < planetQty; planetIndex++) {
            PlanetItem parentPlanet = _planets[planetIndex];
            string[] childMoonDesignNames = Configuration.MoonDesignNames[planetIndex];
            OrbitData[] childMoonOrbitSlots = Configuration.MoonOrbitSlots[planetIndex];
            int childMoonsQty = childMoonDesignNames.Length;
            for (int moonIndex = 0; moonIndex < childMoonsQty; moonIndex++) {
                MoonDesign moonDesign = _gameMgr.CelestialDesigns.GetMoonDesign(childMoonDesignNames[moonIndex]);
                var cameraStat = MakeMoonCameraStat(moonDesign.Stat);
                OrbitData orbitSlot = childMoonOrbitSlots[moonIndex];
                orbitSlot.OrbitedItem = parentPlanet.gameObject;
                MoonItem moon = _systemFactory.MakeInstance(moonDesign, cameraStat, orbitSlot);
                D.Log("{0} has assumed orbit slot {1} around Planet {2}.", moon.FullName, orbitSlot.SlotIndex, parentPlanet.FullName);
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
    //private void AssignPlanetOrbitSlotsToMoons() {
    //    LogEvent();
    //    IList<MoonItem> moonsToDestroy = null;
    //    foreach (var planet in _planets) {
    //        float depthAvailForMoonOrbitsAroundPlanet = _systemOrbitSlotDepth;

    //        float startDepthForMoonOrbitSlot = planet.Data.CloseOrbitOuterRadius;
    //        var moons = planet.GetComponentsInChildren<MoonItem>();
    //        if (moons.Any()) {
    //            int slotIndex = Constants.Zero;
    //            foreach (var moon in moons) {
    //                float depthReqdForMoonOrbitSlot = 2F * moon.ObstacleZoneRadius;
    //                float endDepthForMoonOrbitSlot = startDepthForMoonOrbitSlot + depthReqdForMoonOrbitSlot;
    //                if (endDepthForMoonOrbitSlot <= depthAvailForMoonOrbitsAroundPlanet) {
    //                    moon.Name = planet.Name + _moonLetters[slotIndex];
    //                    GameTimeDuration orbitPeriod = _minMoonOrbitPeriod + (slotIndex * _moonOrbitPeriodIncrement);
    //                    var moonOrbitSlot = new OrbitData(planet.gameObject, startDepthForMoonOrbitSlot, endDepthForMoonOrbitSlot, planet.IsMobile, orbitPeriod);
    //                    _generalFactory.InstallCelestialItemInOrbit(moon.gameObject, moonOrbitSlot);
    //                    //D.Log("{0} has assumed orbit slot {1} around Planet {2}.", moon.FullName, slotIndex, planet.FullName);

    //                    startDepthForMoonOrbitSlot = endDepthForMoonOrbitSlot;
    //                    slotIndex++;
    //                }
    //                else {
    //                    if (moonsToDestroy == null) {
    //                        moonsToDestroy = new List<MoonItem>();
    //                    }
    //                    D.Warn(slotIndex == Constants.Zero, "{0}: Size of planet {1} precludes adding any moons.", GetType().Name, planet.FullName);
    //                    //D.Log("{0} scheduled for destruction. OrbitSlot outer depth {1} > available depth {2}.",
    //                    //    moon.FullName, endDepthForMoonOrbitSlot, depthAvailForMoonOrbitsAroundPlanet);
    //                    moonsToDestroy.Add(moon);
    //                }
    //            }
    //        }
    //    }
    //    if (moonsToDestroy != null) {
    //        moonsToDestroy.ForAll(m => {
    //            _moons.Remove(m);
    //            //D.Log("Destroying Moon {0}.", m.FullName);
    //            Destroy(m.gameObject);
    //        });
    //    }
    //}

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

    private void AddToGameKnowledge() {
        var planetoids = _planets.Cast<IPlanetoid>().Union(_moons.Cast<IPlanetoid>());
        _gameMgr.GameKnowledge.AddSystem(_star, planetoids);
    }

    #endregion

    private void CompleteSystemInitialization() {
        LogEvent();
        _planets.ForAll(p => p.FinalInitialize());
        _moons.ForAll(m => m.FinalInitialize());
        _star.FinalInitialize();
        _system.FinalInitialize();
    }

    private void BeginSystemOperations() {
        LogEvent();
        _planets.ForAll(p => p.CommenceOperations());
        _moons.ForAll(m => m.CommenceOperations());
        _star.CommenceOperations();
        _system.CommenceOperations();
        // 3.25.16 I eliminated the 1 frame delay onCompletion delegate to allow Planetoids Idling_EnterState to execute
        // as I think this is a bad practice. Anyway, 1 frame wouldn't be enough for an IEnumerator Idling_EnterState
        // to finish if it had more than one set of yields in it. In addition, if there is going to be a problem with 
        // changing state when Idling is the state, but its EnterState hasn't run yet, I
        // should find it out now as this can easily happen during the game.
    }

    private void DestroyCreationObject() {
        D.Assert(transform.childCount == 1);
        foreach (Transform child in transform) {
            child.parent = SystemsFolder.Instance.Folder;
        }
        Destroy(gameObject);
    }

    protected FocusableItemCameraStat MakeSystemCameraStat() {
        float minViewDistance = 2F;   // 2 units from the orbital plane
        float optViewDistance = TempGameValues.SystemRadius;
        return new FocusableItemCameraStat(minViewDistance, optViewDistance, fov: 70F);
    }

    protected FocusableItemCameraStat MakeStarCameraStat(StarStat starStat) {
        float minViewDistance = starStat.Radius + 1F;
        float highOrbitRadius = starStat.CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new FocusableItemCameraStat(minViewDistance, optViewDistance, fov: 70F);
    }

    protected FollowableItemCameraStat MakePlanetCameraStat(PlanetStat planetStat) {
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
        float highOrbitRadius = planetStat.CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
    }

    protected FollowableItemCameraStat MakeMoonCameraStat(PlanetoidStat moonStat) {
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
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
    }

    private float __CalcSystemOrbitSlotDepth(StarItem star) {
        float sysOrbitSlotsStartRadius = star.Data.CloseOrbitOuterRadius;
        float systemRadiusAvailableForAllOrbits = TempGameValues.SystemRadius - sysOrbitSlotsStartRadius;
        return systemRadiusAvailableForAllOrbits / (float)TempGameValues.TotalOrbitSlotsPerSystem;
    }



    //protected bool __TryChooseParentPlanet(PlanetoidStat moonStat, out PlanetItem planet) {
    //    PlanetoidCategory[] choicePlanetCats;
    //    PlanetoidCategory moonCat = moonStat.Category;
    //    switch (moonCat) {
    //        case PlanetoidCategory.Moon_005:    // Radius = 1F
    //            choicePlanetCats = new PlanetoidCategory[] { PlanetoidCategory.GasGiant };
    //            break;
    //        case PlanetoidCategory.Moon_004:    // Radius = 0.5F
    //            choicePlanetCats = new PlanetoidCategory[] { PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial };
    //            break;
    //        case PlanetoidCategory.Moon_001:    // Radius = 0.2F
    //        case PlanetoidCategory.Moon_002:    // Radius = 0.2F
    //        case PlanetoidCategory.Moon_003:    // Radius = 0.2F
    //            choicePlanetCats = new PlanetoidCategory[] { PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial, PlanetoidCategory.Volcanic };
    //            break;
    //        case PlanetoidCategory.GasGiant:    // Radius = 5F
    //        case PlanetoidCategory.Ice:         // Radius = 2F
    //        case PlanetoidCategory.Terrestrial: // Radius = 2F
    //        case PlanetoidCategory.Volcanic:    // Radius = 1F
    //        case PlanetoidCategory.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(moonCat));
    //    }

    //    if (!_planets.IsNullOrEmpty()) {
    //        var planetCandidates = _planets.Where(p => choicePlanetCats.Contains(p.category));
    //        if (planetCandidates.Any()) {
    //            planet = planetCandidates.Shuffle().First();
    //            return true;
    //        }
    //    }
    //    planet = null;
    //    return false;
    //}

    protected override void Cleanup() { }

}


