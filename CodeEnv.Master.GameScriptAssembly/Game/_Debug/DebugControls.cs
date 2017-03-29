﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugControls.cs
// Singleton. Editor Debug controls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Editor Debug controls.
/// </summary>
public class DebugControls : AMonoSingleton<DebugControls>, IDebugControls {

    // Notes: Has custom editor that uses NguiEditorTools and SerializedObject. 
    // Allows concurrent use of [Tooltip("")]. NguiEditorTools do not offer a separate tooltip option because this concurrent use is allowed.
    // [Header("")] can also be used concurrently, but this can also be done in the custom editor with greater location precision.

    #region Event Delegates

    public event EventHandler validatePlayerKnowledgeNow;

    public event EventHandler showFleetCoursePlots;

    public event EventHandler showShipCoursePlots;

    public event EventHandler showFleetVelocityRays;

    public event EventHandler showShipVelocityRays;

    public event EventHandler showFleetFormationStations;

    public event EventHandler showShipCollisionDetectionZones;

    public event EventHandler showShields;

    public event EventHandler showSensors;

    public event EventHandler showObstacleZones;

    public event EventHandler showSystemTrackingLabels;

    public event EventHandler showUnitTrackingLabels;

    public event EventHandler showElementIcons;

    public event EventHandler showPlanetIcons;

    public event EventHandler showStarIcons;

    #endregion

    #region Debug Log Editor Fields

    [Tooltip("Check to show debug logs for all ships")]
    [SerializeField]
    private bool _showShipDebugLogs = false;
    public bool ShowShipDebugLogs { get { return _showShipDebugLogs; } }

    [Tooltip("Check to show debug logs for all facilities")]
    [SerializeField]
    private bool _showFacilityDebugLogs = false;
    public bool ShowFacilityDebugLogs { get { return _showFacilityDebugLogs; } }

    [Tooltip("Check to show debug logs for all stars")]
    [SerializeField]
    private bool _showStarDebugLogs = false;
    public bool ShowStarDebugLogs { get { return _showStarDebugLogs; } }

    [Tooltip("Check to show debug logs for all planets and moons")]
    [SerializeField]
    private bool _showPlanetoidDebugLogs = false;
    public bool ShowPlanetoidDebugLogs { get { return _showPlanetoidDebugLogs; } }

    [Tooltip("Check to show debug logs for all Starbase and Settlement Cmds")]
    [SerializeField]
    private bool _showBaseCmdDebugLogs = false;
    public bool ShowBaseCmdDebugLogs { get { return _showBaseCmdDebugLogs; } }

    [Tooltip("Check to show debug logs for all Fleet Cmds")]
    [SerializeField]
    private bool _showFleetCmdDebugLogs = false;
    public bool ShowFleetCmdDebugLogs { get { return _showFleetCmdDebugLogs; } }

    [Tooltip("Check to show debug logs for all Systems")]
    [SerializeField]
    private bool _showSystemDebugLogs = false;
    public bool ShowSystemDebugLogs { get { return _showSystemDebugLogs; } }

    [Tooltip("Check to show debug logs for Deployment (Configurators and Creators)")]
    [SerializeField]
    private bool _showDeploymentDebugLogs = false;
    public bool ShowDeploymentDebugLogs { get { return _showDeploymentDebugLogs; } }

    [Tooltip("Check to show debug logs for Ordnance")]
    [SerializeField]
    private bool _showOrdnanceDebugLogs = false;
    public bool ShowOrdnanceDebugLogs { get { return _showOrdnanceDebugLogs; } }

    #endregion

    #region AI Editor Fields

    [Tooltip("Check if fleets should automatically explore and visit bases without countervailing orders.")]
    [SerializeField]
    private bool _fleetsAutoExplore = true;
    /// <summary>
    /// Indicates whether fleets should automatically explore and visit bases without countervailing orders.
    /// <remarks>10.17.16 The only current source of countervailing orders are from editor fields
    /// on DebugFleetCreators.</remarks>
    /// </summary>
    public bool FleetsAutoExploreAsDefault { get { return _fleetsAutoExplore; } }


    [Tooltip("Check if fleets should automatically attack all other players.")]
    [SerializeField]
    private bool _fleetsAutoAttack = false;
    /// <summary>
    /// Indicates whether fleets should automatically attack all war enemies. All players, when they first
    /// meet will have their relationship set to War. If a fleet can find no enemy to attack, it will explore,
    /// then after exploring, check to see if it can now attack.
    /// <remarks>2.14.17 User Relationship Settings and existing orders for DebugUnitCreators will be ignored
    /// as all players will have their relationship set to War when they first meet.</remarks>
    /// </summary>
    public bool FleetsAutoAttackAsDefault { get { return _fleetsAutoAttack; } }

    [Tooltip("The maximum number of fleets that can be concurrently attacking per player")]
    [Range(1, 3)]
    [SerializeField]
    private int _maxAttackingFleetsPerPlayer = 2;
    /// <summary>
    /// The maximum number of concurrently attacking fleets allowed per player.
    /// </summary>
    public int MaxAttackingFleetsPerPlayer { get { return _maxAttackingFleetsPerPlayer; } }

    [Tooltip("Check if all players know everything about all objects, even those that are outside detection range.")]
    [SerializeField]
    private bool _allIntelCoverageIsComprehensive = false;
    /// <summary>
    /// If <c>true</c> every player knows everything about every item independent of whether
    /// their sensors have detected the item. 
    /// <remarks>It also means that all players have met as Players meet when they first become
    /// aware of a Cmd owned by another player.</remarks>
    /// </summary>
    public bool IsAllIntelCoverageComprehensive { get { return _allIntelCoverageIsComprehensive; } }

    #endregion

    #region Audio Editor Fields

    [Tooltip("Check if Weapon Impact SFX should always be heard, even when the visual effect may not show")]
    [SerializeField]
    private bool _alwaysHearWeaponImpacts = false;
    /// <summary>
    /// Indicates whether weapon impact SFX should always be heard, even when the 
    /// visual effect is not showing.
    /// </summary>
    public bool AlwaysHearWeaponImpacts { get { return _alwaysHearWeaponImpacts; } }

    #endregion

    #region General Editor Fields

    [Tooltip("Check if world tracking sprites and labels should use one UIPanel per widget")]
    [SerializeField]
    private bool _useOneUIPanelPerWidget = false;
    /// <summary>
    /// If <c>true</c> all world tracking sprites and labels will use one UIPanel per widget,
    /// otherwise all world tracking sprites and labels will be consolidated under a few UIPanels.
    /// </summary>
    public bool UseOneUIPanelPerWidget { get { return _useOneUIPanelPerWidget; } }

    #endregion

    #region InGame Display Editor Fields

    [Tooltip("Shows the course plotted for each Fleet")]
    [SerializeField]
    private bool _showFleetCoursePlots = false;
    public bool ShowFleetCoursePlots { get { return _showFleetCoursePlots; } }

    [Tooltip("Shows the course plotted for each Ship")]
    [SerializeField]
    private bool _showShipCoursePlots = false;
    public bool ShowShipCoursePlots { get { return _showShipCoursePlots; } }

    [Tooltip("Shows the velocity and direction of travel for each Fleet")]
    [SerializeField]
    private bool _showFleetVelocityRays = false;
    public bool ShowFleetVelocityRays { get { return _showFleetVelocityRays; } }

    [Tooltip("Shows the velocity and direction of travel for each Ship")]
    [SerializeField]
    private bool _showShipVelocityRays = false;
    public bool ShowShipVelocityRays { get { return _showShipVelocityRays; } }

    [Tooltip("Shows the collision detection zones for each Ship")]
    [SerializeField]
    private bool _showShipCollisionDetectionZones = false;
    public bool ShowShipCollisionDetectionZones { get { return _showShipCollisionDetectionZones; } }

    [Tooltip("Shows the formation station of each Ship")]
    [SerializeField]
    private bool _showFleetFormationStations = false;
    public bool ShowFleetFormationStations { get { return _showFleetFormationStations; } }

    [Tooltip("Shows the shields of each Element")]
    [SerializeField]
    private bool _showShields = false;
    public bool ShowShields { get { return _showShields; } }

    [Tooltip("Shows the sensors for each Unit")]
    [SerializeField]
    private bool _showSensors = false;
    public bool ShowSensors { get { return _showSensors; } }

    [Tooltip("Shows the obstacle zones surrounding each avoidable obstacle")]
    [SerializeField]
    private bool _showObstacleZones = false;
    public bool ShowObstacleZones { get { return _showObstacleZones; } }

    [Tooltip("Shows the tracking label for each unit")]
    [SerializeField]
    private bool _showUnitTrackingLabels = false;
    public bool ShowUnitTrackingLabels { get { return _showUnitTrackingLabels; } }

    [Tooltip("Shows the tracking label for each system")]
    [SerializeField]
    private bool _showSystemTrackingLabels = false;
    public bool ShowSystemTrackingLabels { get { return _showSystemTrackingLabels; } }

    [Tooltip("Check if Icons for Elements should show")]
    [SerializeField]
    private bool _showElementIcons = true;
    /// <summary>
    /// If <c>true</c> elements will display 2D icons when the camera is too far away to discern the mesh.
    /// </summary>
    public bool ShowElementIcons { get { return _showElementIcons; } }

    [Tooltip("Check if Icons for Planets should show")]
    [SerializeField]
    private bool _showPlanetIcons = true;
    /// <summary>
    /// If <c>true</c> planets will display 2D icons when the camera is too far away to discern the mesh.
    /// </summary>
    public bool ShowPlanetIcons { get { return _showPlanetIcons; } }

    [Tooltip("Check if Icons for Stars should show")]
    [SerializeField]
    private bool _showStarIcons = true;
    /// <summary>
    /// If <c>true</c> stars will display 2D icons when the camera is too far away to discern the mesh.
    /// </summary>
    public bool ShowStarIcons { get { return _showStarIcons; } }

    #endregion


    public override bool IsPersistentAcrossScenes { get { return true; } }  // GameScene -> GameScene retains values

    private string Name { get { return GetType().Name; } }

    private IGameManager _gameMgr;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.DebugControls = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameReferences.GameManager;
    }

    #endregion

    #region Value Change Checking

    void OnValidate() {
        CheckValuesForChange();
    }

    private void CheckValuesForChange() {
        CheckShowFleetCoursePlots();
        CheckShowShipCoursePlots();
        CheckShowFleetVelocityRays();
        CheckShowShipVelocityRays();
        CheckShowFleetFormationStations();
        CheckShowShipCollisionDetectionZones();
        CheckShowShields();
        CheckShowSensors();
        CheckShowObstacleZones();

        CheckShowSystemTrackingLabels();
        CheckShowUnitTrackingLabels();

        CheckShowElementIcons();
        CheckShowPlanetIcons();
        CheckShowStarIcons();
    }

    private bool _showFleetCoursePlotsPrev = false;
    private void CheckShowFleetCoursePlots() {
        if (_showFleetCoursePlots != _showFleetCoursePlotsPrev) {
            //D.Log("{0}.ShowFleetCoursePlots has changed from {1} to {2}.", DebugName, _showFleetCoursePlotsPrev, _showFleetCoursePlots);
            _showFleetCoursePlotsPrev = _showFleetCoursePlots;
            OnShowFleetCoursePlotsChanged();
        }
    }

    private bool _showShipCoursePlotsPrev = false;
    private void CheckShowShipCoursePlots() {
        if (_showShipCoursePlots != _showShipCoursePlotsPrev) {
            //D.Log("{0}.ShowShipCoursePlots has changed from {1} to {2}.", DebugName, _showShipCoursePlotsPrev, _showShipCoursePlots);
            _showShipCoursePlotsPrev = _showShipCoursePlots;
            OnShowShipCoursePlotsChanged();
        }
    }

    private bool _showFleetVelocityRaysPrev = false;
    private void CheckShowFleetVelocityRays() {
        if (_showFleetVelocityRays != _showFleetVelocityRaysPrev) {
            //D.Log("{0}.ShowFleetVelocityRays has changed from {1} to {2}.", DebugName, _showFleetVelocityRaysPrev, _showFleetVelocityRays);
            _showFleetVelocityRaysPrev = _showFleetVelocityRays;
            OnShowFleetVelocityRaysChanged();
        }
    }

    private bool _showShipVelocityRaysPrev = false;
    private void CheckShowShipVelocityRays() {
        if (_showShipVelocityRays != _showShipVelocityRaysPrev) {
            //D.Log("{0}.ShowShipVelocityRays has changed from {1} to {2}.", DebugName, _showShipVelocityRaysPrev, _showShipVelocityRays);
            _showShipVelocityRaysPrev = _showShipVelocityRays;
            OnShowShipVelocityRaysChanged();
        }
    }

    private bool _showFleetFormationStationsPrev;
    private void CheckShowFleetFormationStations() {
        if (_showFleetFormationStations != _showFleetFormationStationsPrev) {
            //D.Log("{0}.ShowShipFormationStations has changed from {1} to {2}.", DebugName, _showFleetFormationStationsPrev, _showFleetFormationStations);
            _showFleetFormationStationsPrev = _showFleetFormationStations;
            OnShowFormationStationsChanged();
        }
    }

    private bool _showShipCollisionDetectionZonesPrev;
    private void CheckShowShipCollisionDetectionZones() {
        if (_showShipCollisionDetectionZones != _showShipCollisionDetectionZonesPrev) {
            //D.Log("{0}.ShowShipCollisionDetectionZones has changed from {1} to {2}.", DebugName, _showShipCollisionDetectionZonesPrev, _showShipCollisionDetectionZones);
            _showShipCollisionDetectionZonesPrev = _showShipCollisionDetectionZones;
            OnShowShipCollisionDetectionZonesChanged();
        }
    }

    private bool _showShieldsPrev;
    private void CheckShowShields() {
        if (_showShields != _showShieldsPrev) {
            //D.Log("{0}.ShowShields has changed from {1} to {2}.", DebugName, _showShieldsPrev, _showShields);
            _showShieldsPrev = _showShields;
            OnShowShieldsChanged();
        }
    }

    private bool _showSensorsPrev;
    private void CheckShowSensors() {
        if (_showSensors != _showSensorsPrev) {
            //D.Log("{0}.ShowSensors has changed from {1} to {2}.", DebugName, _showSensorsPrev, _showSensors);
            _showSensorsPrev = _showSensors;
            OnShowSensorsChanged();
        }
    }

    private bool _showObstacleZonesPrev;
    private void CheckShowObstacleZones() {
        if (_showObstacleZones != _showObstacleZonesPrev) {
            //D.Log("{0}.ShowObstacleZones has changed from {1} to {2}.", DebugName, _showObstacleZonesPrev, _showObstacleZones);
            _showObstacleZonesPrev = _showObstacleZones;
            OnShowObstacleZonesChanged();
        }
    }

    private bool _showSystemTrackingLabelsPrev;
    private void CheckShowSystemTrackingLabels() {
        if (_showSystemTrackingLabels != _showSystemTrackingLabelsPrev) {
            //D.Log("{0}.ShowSystemTrackingLabels has changed from {1} to {2}.", DebugName, _showSystemTrackingLabelsPrev, _showSystemTrackingLabels);
            _showSystemTrackingLabelsPrev = _showSystemTrackingLabels;
            OnShowSystemTrackingLabels();
        }
    }

    private bool _showUnitTrackingLabelsPrev;
    private void CheckShowUnitTrackingLabels() {
        if (_showUnitTrackingLabels != _showUnitTrackingLabelsPrev) {
            //D.Log("{0}.ShowUnitTrackingLabels has changed from {1} to {2}.", DebugName, _showUnitTrackingLabelsPrev, _showUnitTrackingLabels);
            _showUnitTrackingLabelsPrev = _showUnitTrackingLabels;
            OnShowUnitTrackingLabels();
        }
    }

    private bool _showElementIconsPrev;
    private void CheckShowElementIcons() {
        if (_showElementIcons != _showElementIconsPrev) {
            //D.Log("{0}.ShowElementIcons has changed from {1} to {2}.", DebugName, _showElementIconsPrev, _showElementIcons);
            _showElementIconsPrev = _showElementIcons;
            OnShowElementIcons();
        }
    }

    private bool _showPlanetIconsPrev;
    private void CheckShowPlanetIcons() {
        if (_showPlanetIcons != _showPlanetIconsPrev) {
            //D.Log("{0}.ShowPlanetIcons has changed from {1} to {2}.", DebugName, _showPlanetIconsPrev, _showPlanetIcons);
            _showPlanetIconsPrev = _showPlanetIcons;
            OnShowPlanetIcons();
        }
    }

    private bool _showStarIconsPrev;
    private void CheckShowStarIcons() {
        if (_showStarIcons != _showStarIconsPrev) {
            //D.Log("{0}.ShowStarIcons has changed from {1} to {2}.", DebugName, _showStarIconsPrev, _showStarIcons);
            _showStarIconsPrev = _showStarIcons;
            OnShowStarIcons();
        }
    }

    #endregion

    #region Event and Prop Change Handlers

    private void OnShowFleetCoursePlotsChanged() {
        if (showFleetCoursePlots != null) {
            showFleetCoursePlots(Instance, EventArgs.Empty);
        }
    }

    private void OnShowShipCoursePlotsChanged() {
        if (showShipCoursePlots != null) {
            showShipCoursePlots(Instance, EventArgs.Empty);
        }
    }

    private void OnShowFleetVelocityRaysChanged() {
        if (showFleetVelocityRays != null) {
            showFleetVelocityRays(Instance, EventArgs.Empty);
        }
    }

    private void OnShowShipVelocityRaysChanged() {
        if (showShipVelocityRays != null) {
            showShipVelocityRays(Instance, EventArgs.Empty);
        }
    }

    private void OnShowFormationStationsChanged() {
        if (showFleetFormationStations != null) {
            showFleetFormationStations(Instance, EventArgs.Empty);
        }
    }

    private void OnShowShipCollisionDetectionZonesChanged() {
        if (showShipCollisionDetectionZones != null) {
            showShipCollisionDetectionZones(Instance, EventArgs.Empty);
        }
    }

    private void OnShowShieldsChanged() {
        if (showShields != null) {
            showShields(Instance, EventArgs.Empty);
        }
    }

    private void OnShowSensorsChanged() {
        if (showSensors != null) {
            showSensors(Instance, EventArgs.Empty);
        }
    }

    private void OnShowObstacleZonesChanged() {
        if (showObstacleZones != null) {
            showObstacleZones(Instance, EventArgs.Empty);
        }
    }

    public void OnValidatePlayerKnowledgeNow() {
        if (validatePlayerKnowledgeNow != null) {
            validatePlayerKnowledgeNow(Instance, EventArgs.Empty);
        }
    }

    private void OnShowSystemTrackingLabels() {
        if (showSystemTrackingLabels != null) {
            showSystemTrackingLabels(Instance, EventArgs.Empty);
        }
    }

    private void OnShowUnitTrackingLabels() {
        if (showUnitTrackingLabels != null) {
            showUnitTrackingLabels(Instance, EventArgs.Empty);
        }
    }

    private void OnShowElementIcons() {
        if (showElementIcons != null) {
            showElementIcons(Instance, EventArgs.Empty);
        }
    }

    private void OnShowPlanetIcons() {
        if (showPlanetIcons != null) {
            showPlanetIcons(Instance, EventArgs.Empty);
        }
    }

    private void OnShowStarIcons() {
        if (showStarIcons != null) {
            showStarIcons(Instance, EventArgs.Empty);
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        GameReferences.DebugControls = null;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

