// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreator.cs
// Creates Systems derived from a SystemCreatorConfiguration.
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
/// Creates Systems derived from a SystemCreatorConfiguration. Base class for DebugSystemCreator
/// used in the Editor.
/// </summary>
public class SystemCreator : AMonoBase {

    private const string NameFormat = "{0}.{1}";

    public string Name { get { return NameFormat.Inject(SystemName, GetType().Name); } }

    public string SystemName {
        get { return transform.name; }
        private set { transform.name = value; }
    }

    public IntVector3 SectorIndex { get { return SectorGrid.Instance.GetSectorIndexThatContains(transform.position); } }


    private SystemCreatorConfiguration _configuration;
    public SystemCreatorConfiguration Configuration {
        get { return _configuration; }
        set {
            D.Assert(_configuration == null); // should only occur one time
            SetProperty<SystemCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationSetHandler);
        }
    }

    protected SystemItem _system;
    protected StarItem _star;
    protected IList<PlanetItem> _planets;
    protected IList<MoonItem> _moons;

    protected GameManager _gameMgr;
    protected SystemFactory _systemFactory;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _systemFactory = SystemFactory.Instance;
    }

    #region Event and Property Change Handlers

    protected override void Start() {
        base.Start();
        D.Assert(gameObject.isStatic, "{0} should be static after being positioned.", GetType().Name);
    }

    private void ConfigurationSetHandler() {
        SystemName = Configuration.SystemName;
    }

    #endregion

    // 10.2.16 GameState change event handlers eliminated. Now exposes methods to NewGameSystemConfigurator

    #region Build and Deploy System

    public void BuildAndDeploySystem() {
        LogEvent();
        MakeSystem();   // stars and planets need a system parent when built
        MakeStar();     // makes the star a child of the system
        MakePlanetsAndPlaceInOrbits();
        MakeMoonsAndPlaceInOrbits();
        AddMembersToSystem();
        AddToGameKnowledge();
    }

    protected virtual void MakeSystem() {
        LogEvent();
        FocusableItemCameraStat cameraStat = MakeSystemCameraStat();
        _system = _systemFactory.MakeSystemInstance(SystemName, gameObject, cameraStat);
        D.Assert(_system.gameObject.isStatic, "{0} should be static after being positioned.", _system.FullName);

        _system.SettlementOrbitData = InitializeSettlementOrbitSlot();
        _system.IsTrackingLabelEnabled = Configuration.IsTrackingLabelEnabled;
        SectorGrid.Instance.GetSector(_system.SectorIndex).System = _system;
    }

    protected OrbitData InitializeSettlementOrbitSlot() {
        var orbitSlot = Configuration.SettlementOrbitSlot;
        orbitSlot.AssignOrbitedItem(_system.gameObject, _system.IsMobile);
        orbitSlot.ToOrbit = TempGameValues.DoSettlementsActivelyOrbit;
        return orbitSlot;
    }

    protected virtual void MakeStar() {
        LogEvent();
        StarDesign design = _gameMgr.CelestialDesigns.GetStarDesign(Configuration.StarDesignName);
        FocusableItemCameraStat cameraStat = MakeStarCameraStat(design.StarStat);
        _star = _systemFactory.MakeInstance(design, cameraStat, _system.gameObject, SystemName);
    }

    protected virtual void MakePlanetsAndPlaceInOrbits() {
        LogEvent();
        int planetQty = Configuration.PlanetDesignNames.Count;
        _planets = new List<PlanetItem>(planetQty);
        for (int index = 0; index < planetQty; index++) {
            string designName = Configuration.PlanetDesignNames[index];
            PlanetDesign design = _gameMgr.CelestialDesigns.GetPlanetDesign(designName);
            FollowableItemCameraStat cameraStat = MakePlanetCameraStat(design.Stat);
            OrbitData planetOrbitSlot = Configuration.PlanetOrbitSlots[index];
            planetOrbitSlot.AssignOrbitedItem(_system.gameObject, _system.IsMobile);    // UNCLEAR orbit star? 
            planetOrbitSlot.ToOrbit = true;
            var planet = _systemFactory.MakeInstance(design, cameraStat, planetOrbitSlot);
            float sysOrbitSlotDepth;
            D.Warn(planet.Data.CloseOrbitOuterRadius > (sysOrbitSlotDepth = __CalcSystemOrbitSlotDepth(_star)), "{0}: {1} reqd orbit slot depth of {2:0.#} > SystemOrbitSlotDepth of {3:0.#}.",
                Name, planet.FullName, planet.Data.CloseOrbitOuterRadius, sysOrbitSlotDepth);
            //D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, planetOrbitSlot.SlotIndex, SystemName);
            _planets.Add(planet);
        }
    }

    protected virtual void MakeMoonsAndPlaceInOrbits() {
        LogEvent();
        int moonQty = Configuration.MoonDesignNames.SelectMany(m => m).Count();
        _moons = new List<MoonItem>(moonQty);
        int planetQty = _planets.Count;
        for (int planetIndex = 0; planetIndex < planetQty; planetIndex++) {
            PlanetItem aPlanet = _planets[planetIndex];
            string[] aPlanetsChildMoonDesignNames = Configuration.MoonDesignNames[planetIndex];
            OrbitData[] aPlanetsChildMoonOrbitSlots = Configuration.MoonOrbitSlots[planetIndex];
            int aPlanetsChildMoonQty = aPlanetsChildMoonDesignNames.Length;
            for (int moonIndex = 0; moonIndex < aPlanetsChildMoonQty; moonIndex++) {
                MoonDesign moonDesign = _gameMgr.CelestialDesigns.GetMoonDesign(aPlanetsChildMoonDesignNames[moonIndex]);
                var cameraStat = MakeMoonCameraStat(moonDesign.Stat);
                OrbitData orbitSlot = aPlanetsChildMoonOrbitSlots[moonIndex];
                orbitSlot.AssignOrbitedItem(aPlanet.gameObject, aPlanet.IsMobile);
                orbitSlot.ToOrbit = true;
                MoonItem moon = _systemFactory.MakeInstance(moonDesign, cameraStat, orbitSlot);
                //D.Log("{0} has assumed orbit slot {1} around {2}.", moon.FullName, orbitSlot.SlotIndex, aPlanet.FullName);
                _moons.Add(moon);
            }
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

    private void AddToGameKnowledge() {
        var planetoids = _planets.Cast<IPlanetoid>().Union(_moons.Cast<IPlanetoid>());
        _gameMgr.GameKnowledge.AddSystem(_star, planetoids);
    }

    #endregion

    public void CompleteSystemInitialization() {
        LogEvent();
        _planets.ForAll(p => p.FinalInitialize());
        _moons.ForAll(m => m.FinalInitialize());
        _star.FinalInitialize();
        _system.FinalInitialize();

        InitializeTopographyMonitor();
    }

    public void CommenceSystemOperations() {
        LogEvent();
        _planets.ForAll(p => p.CommenceOperations());
        _moons.ForAll(m => m.CommenceOperations());
        _star.CommenceOperations();
        _system.CommenceOperations();

        RemoveCreationObject();
        // 3.25.16 I eliminated the 1 frame delay onCompletion delegate to allow Planetoids Idling_EnterState to execute
        // as I think this is a bad practice. Anyway, 1 frame wouldn't be enough for an IEnumerator Idling_EnterState
        // to finish if it had more than one set of yields in it. In addition, if there is going to be a problem with 
        // changing state when Idling is the state, but its EnterState hasn't run yet, I
        // should find it out now as this can easily happen during the game.
    }

    private void RemoveCreationObject() {
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

    protected float __CalcSystemOrbitSlotDepth(StarItem star) {
        float sysOrbitSlotsStartRadius = star.Data.CloseOrbitOuterRadius;
        float systemRadiusAvailableForAllOrbits = TempGameValues.SystemRadius - sysOrbitSlotsStartRadius;
        return systemRadiusAvailableForAllOrbits / (float)TempGameValues.TotalOrbitSlotsPerSystem;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


