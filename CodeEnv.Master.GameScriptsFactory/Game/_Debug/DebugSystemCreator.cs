// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemCreator.cs
// Creates Systems for debugging in the editor derived from a SystemCreatorConfiguration. 
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
/// Creates Systems for debugging in the editor derived from a SystemCreatorConfiguration. 
/// </summary>
[ExecuteInEditMode] // auto detects preset composition
public class DebugSystemCreator : SystemCreator {

    private const int MaxPlanetsPerSystem = TempGameValues.TotalOrbitSlotsPerSystem - 1;

    #region Serialized Editor Fields

    [SerializeField]
    private bool _isCompositionPreset = false;

    [Range(0, MaxPlanetsPerSystem)]
    [SerializeField]
    private int _planetsInSystem = 3;

    //[Range(0, 2)]
    //[SerializeField]
    //private int _countermeasuresPerPlanetoid = 1;


    [SerializeField]
    private DebugSystemDesirability _desirability = DebugSystemDesirability.Normal;

    [Tooltip("Shows a label containing the System name")]
    [SerializeField]
    private bool _enableTrackingLabel = false;

    #endregion

    private SystemCreatorEditorSettings _editorSettings;
    public SystemCreatorEditorSettings EditorSettings {
        get {
            if (_editorSettings == null) {
                if (_isCompositionPreset) {
                    StarCategory starCategory = GetComponentInChildren<StarItem>().category;
                    PlanetItem[] planets = GetComponentsInChildren<PlanetItem>();
                    IList<PlanetoidCategory> presetPlanetCategories = planets.Select(p => p.category).ToList();
                    int planetQty = presetPlanetCategories.Count;
                    IList<PlanetoidCategory[]> presetMoonCategories = new List<PlanetoidCategory[]>(planetQty);
                    for (int planetIndex = 0; planetIndex < planetQty; planetIndex++) {
                        PlanetItem planet = planets[planetIndex];
                        PlanetoidCategory[] childMoonCategories = planet.GetComponentsInChildren<MoonItem>().Select(m => m.category).ToArray();
                        //D.Log("{0} assigning childMoonCategories to presetMoonCategories at index {1}.", Name, planetIndex);
                        presetMoonCategories.Add(childMoonCategories);  //presetMoonCategories[planetIndex] = childMoonCategories;

                    }
                    _editorSettings = new SystemCreatorEditorSettings(starCategory, presetPlanetCategories, presetMoonCategories, /*_countermeasuresPerPlanetoid,*/_desirability.Convert(), _enableTrackingLabel);
                }
                else {
                    _editorSettings = new SystemCreatorEditorSettings(_planetsInSystem, /*_countermeasuresPerPlanetoid,*/_desirability.Convert(), _enableTrackingLabel);
                }
            }
            return _editorSettings;
        }
    }

    #region ExecuteInEditMode

    protected override void Awake() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.Awake();
    }

    protected override void Update() {
        if (Application.isPlaying) {
            enabled = false;    // Uses ExecuteInEditMode
        }
        base.Update();

        int activePlanetCount = GetComponentsInChildren<PlanetItem>().Count();
        bool hasActivePlanets = activePlanetCount > Constants.Zero;
        D.Assert(hasActivePlanets == (transform.childCount > Constants.Zero), "{0} planets not properly configured.", Name);
        if (hasActivePlanets) {
            _isCompositionPreset = true;
            __AdjustPlanetQtyFieldTo(activePlanetCount);
        }
    }

    protected override void OnDestroy() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.OnDestroy();
    }

    #endregion


    // 10.12.16 Eliminated overridden BuildAndDeploySystem() which checked ValidateConfiguration() as un-configured DebugSystemCreators
    // are destroyed by NewGameSystemConfigurator. It makes no sense for UniverseCreator to call BuildAndDeploySystem on a Creator that
    // hasn't been used and configured

    //public override void BuildAndDeploySystem() {
    //    if (!ValidateConfiguration()) {  // only DebugCreators can be deployed without being assigned a configuration
    //        return;
    //    }
    //    base.BuildAndDeploySystem();
    //}

    //private bool ValidateConfiguration() {
    //    if (Configuration == null) {
    //        D.Error("{0} found Configuration unassigned. Destroying system.", Name);
    //        Destroy(gameObject);
    //        return false;
    //    }
    //    return true;
    //}

    protected override void MakeSystem() {
        if (_isCompositionPreset) {
            LogEvent();

            _system = GetComponentInChildren<SystemItem>();
            FocusableItemCameraStat cameraStat = MakeSystemCameraStat();

            _systemFactory.PopulateSystemInstance(SystemName, cameraStat, ref _system);
            D.Assert(_system.gameObject.isStatic, "{0} should be static after being positioned.", _system.FullName);

            _system.SettlementOrbitData = InitializeSettlementOrbitSlot();
            _system.IsTrackingLabelEnabled = Configuration.IsTrackingLabelEnabled;
            SectorGrid.Instance.GetSector(_system.SectorIndex).System = _system;
        }
        else {
            base.MakeSystem();
        }
    }

    protected override void MakeStar() {
        if (_isCompositionPreset) {
            _star = GetComponentInChildren<StarItem>();
            StarDesign design = _gameMgr.CelestialDesigns.GetStarDesign(Configuration.StarDesignName);
            FocusableItemCameraStat cameraStat = MakeStarCameraStat(design.StarStat);
            _systemFactory.PopulateInstance(design, cameraStat, SystemName, ref _star);
        }
        else {
            base.MakeStar();
        }
    }

    protected override void MakePlanetsAndPlaceInOrbits() {
        if (_isCompositionPreset) {
            LogEvent();
            int planetQty = Configuration.PlanetDesignNames.Count;
            _planets = GetComponentsInChildren<PlanetItem>().ToList();
            IList<PlanetItem> populatedPlanets = new List<PlanetItem>(planetQty);
            for (int index = 0; index < planetQty; index++) {
                string planetDesignName = Configuration.PlanetDesignNames[index];
                PlanetDesign planetDesign = _gameMgr.CelestialDesigns.GetPlanetDesign(planetDesignName);
                FollowableItemCameraStat cameraStat = MakePlanetCameraStat(planetDesign.Stat);
                OrbitData planetOrbitSlot = Configuration.PlanetOrbitSlots[index];
                planetOrbitSlot.AssignOrbitedItem(_system.gameObject, _system.IsMobile);    // UNCLEAR orbit star?
                planetOrbitSlot.ToOrbit = true;
                var planet = _planets.Where(p => p.category == planetDesign.Stat.Category).Except(populatedPlanets).First();
                _systemFactory.PopulateInstance(planetDesign, cameraStat, planetOrbitSlot, ref planet);
                populatedPlanets.Add(planet);
                float sysOrbitSlotDepth;
                D.Warn(planet.Data.CloseOrbitOuterRadius > (sysOrbitSlotDepth = __CalcSystemOrbitSlotDepth(_star)), "{0}: {1} reqd orbit slot depth of {2:0.#} > SystemOrbitSlotDepth of {3:0.#}.",
                    Name, planet.FullName, planet.Data.CloseOrbitOuterRadius, sysOrbitSlotDepth);
                //D.Log("{0} has assumed orbit slot {1} in System {2}.", planet.FullName, planetOrbitSlot.SlotIndex, SystemName);
            }

            // A planet design and orbit slot can be left out of Configuration during its creation if there aren't enough
            // slots of the right type to accommodate the mix of planet categories. If preset the extra planet(s) must be removed.
            var extraPlanets = _planets.Except(populatedPlanets);
            if (extraPlanets.Any()) {
                extraPlanets.ForAll(p => {
                    _planets.Remove(p);
                    D.Warn("{0} is destroying extra preset planet {1}. Check the preset design.", Name, p.FullName);
                    Destroy(p.gameObject);
                });
            }
        }
        else {
            base.MakePlanetsAndPlaceInOrbits();
        }
    }

    protected override void MakeMoonsAndPlaceInOrbits() {
        if (_isCompositionPreset) {
            LogEvent();
            _moons = GetComponentsInChildren<MoonItem>().ToList();
            IList<MoonItem> populatedMoons = new List<MoonItem>(_moons.Count);
            int planetQty = _planets.Count;
            for (int planetIndex = 0; planetIndex < planetQty; planetIndex++) {
                PlanetItem aPlanet = _planets[planetIndex];
                string[] aPlanetsChildMoonDesignNames = Configuration.MoonDesignNames[planetIndex];
                OrbitData[] aPlanetsChildMoonOrbitSlots = Configuration.MoonOrbitSlots[planetIndex];
                var aPlanetsPresetMoons = aPlanet.GetComponentsInChildren<MoonItem>();
                int aPlanetsMoonDesignsQty = aPlanetsChildMoonDesignNames.Length;
                for (int moonIndex = 0; moonIndex < aPlanetsMoonDesignsQty; moonIndex++) {
                    string moonDesignName = aPlanetsChildMoonDesignNames[moonIndex];
                    MoonDesign moonDesign = _gameMgr.CelestialDesigns.GetMoonDesign(moonDesignName);
                    var cameraStat = MakeMoonCameraStat(moonDesign.Stat);

                    OrbitData moonOrbitSlot = aPlanetsChildMoonOrbitSlots[moonIndex];
                    moonOrbitSlot.AssignOrbitedItem(aPlanet.gameObject, aPlanet.IsMobile);
                    moonOrbitSlot.ToOrbit = true;

                    MoonItem moon = aPlanetsPresetMoons.Where(m => m.category == moonDesign.Stat.Category).Except(populatedMoons).First();
                    _systemFactory.PopulateInstance(moonDesign, cameraStat, moonOrbitSlot, ref moon);
                    populatedMoons.Add(moon);
                    //D.Log("{0} has assumed orbit slot {1} around Planet {2}.", moon.FullName, moonOrbitSlot.SlotIndex, aPlanet.FullName);
                }
            }

            // A moon design and orbit slot can be left out of Configuration during its creation if there are too many moons
            // or they are too large to fit around the planet. If preset the extra moon(s) must be removed.
            var extraMoons = _moons.Except(populatedMoons);
            if (extraMoons.Any()) {
                extraMoons.ForAll(m => {
                    _moons.Remove(m);
                    D.Warn("{0} is destroying extra preset moon {1}. Check the preset design.", Name, m.FullName);
                    Destroy(m.gameObject);
                });
            }
        }
        else {
            base.MakeMoonsAndPlaceInOrbits();
        }
    }

    private void __AdjustPlanetQtyFieldTo(int qty) {
        _planetsInSystem = qty;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

