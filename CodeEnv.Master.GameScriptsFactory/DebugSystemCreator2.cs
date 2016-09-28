// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemCreator2.cs
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
[ExecuteInEditMode] // auto detects preset composition
public class DebugSystemCreator2 : SystemCreator2 {

    private const int MaxPlanetsPerSystem = TempGameValues.TotalOrbitSlotsPerSystem - 1;
    private const int MaxMoonsPerSystem = MaxPlanetsPerSystem * 2;


    #region Serialized Editor Fields

    [SerializeField]
    private bool _isCompositionPreset = false;

    [Range(0, MaxPlanetsPerSystem)]
    [SerializeField]
    private int _maxPlanetsInSystem = 3;

    [Range(0, MaxMoonsPerSystem)]
    [SerializeField]
    private int _maxMoonsInSystem = 3;

    [Range(0, 2)]
    [SerializeField]
    private int _countermeasuresPerPlanetoid = 1;

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
                    IList<PlanetoidCategory> presetPlanetCategories = GetComponentsInChildren<PlanetItem>().Select(p => p.category).ToList();
                    int planetQty = presetPlanetCategories.Count;
                    IList<PlanetoidCategory[]> presetMoonCategories = new List<PlanetoidCategory[]>(planetQty);
                    for (int planetIndex = 0; planetIndex < planetQty; planetIndex++) {
                        var childMoonCategories = GetComponentsInChildren<MoonItem>().Select(m => m.category).ToArray();
                        presetMoonCategories.Add(childMoonCategories);
                    }
                    _editorSettings = new SystemCreatorEditorSettings(SystemName, starCategory, presetPlanetCategories, presetMoonCategories, _countermeasuresPerPlanetoid, _enableTrackingLabel);
                }
                else {
                    _editorSettings = new SystemCreatorEditorSettings(SystemName, _maxPlanetsInSystem, _maxMoonsInSystem, _countermeasuresPerPlanetoid, _enableTrackingLabel);
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
            int activeMoonCount = GetComponentsInChildren<MoonItem>().Count();
            __AdjustMoonQtyFieldTo(activeMoonCount);
        }
    }

    protected override void OnDestroy() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.OnDestroy();
    }

    #endregion

    protected override void MakeSystem() {
        if (_isCompositionPreset) {
            LogEvent();

            _system = GetComponentInChildren<SystemItem>();
            FocusableItemCameraStat cameraStat = MakeSystemCameraStat();

            _systemFactory.PopulateSystemInstance(SystemName, cameraStat, ref _system);
            D.Assert(_system.gameObject.isStatic, "{0} should be static after being positioned.", _system.FullName);

            _system.SettlementOrbitData = Configuration.SettlementOrbitSlot;
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



    private void __AdjustPlanetQtyFieldTo(int qty) {
        _maxPlanetsInSystem = qty;
    }

    private void __AdjustMoonQtyFieldTo(int qty) {
        _maxMoonsInSystem = qty;
    }



    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

