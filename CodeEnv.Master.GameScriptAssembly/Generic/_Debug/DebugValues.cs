// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugValues.cs
// Singleton. Editor Debug values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton. Editor Debug values.
/// </summary>
public class DebugValues : AMonoSingleton<DebugValues> {

    public EventHandler showFleetCoursePlotsChanged;

    public EventHandler showShipCoursePlotsChanged;

    public EventHandler showFleetVelocityRaysChanged;

    public EventHandler showShipVelocityRaysChanged;

    public EventHandler showFleetFormationStationsChanged;

    public EventHandler showShipCollisionDetectionZonesChanged;

    public EventHandler showShieldsChanged;

    public EventHandler showSensorsChanged;

    public EventHandler showObstacleZonesChanged;

    [Header("Display CoursePlots")]
    [Tooltip("Show Fleet Course Plots")]
    [SerializeField]
    private bool _showFleetCoursePlots = false;
    public bool ShowFleetCoursePlots { get { return _showFleetCoursePlots; } }

    [Tooltip("Show Ship Course Plots")]
    [SerializeField]
    private bool _showShipCoursePlots = false;
    public bool ShowShipCoursePlots { get { return _showShipCoursePlots; } }

    [Header("Display VelocityRays")]
    [Tooltip("Show Fleet Velocity Rays")]
    [SerializeField]
    private bool _showFleetVelocityRays = false;
    public bool ShowFleetVelocityRays { get { return _showFleetVelocityRays; } }

    [Tooltip("Show Ship Velocity Rays")]
    [SerializeField]
    private bool _showShipVelocityRays = false;
    public bool ShowShipVelocityRays { get { return _showShipVelocityRays; } }

    [Header("Display Volumes")]
    [Tooltip("Show Ship Collision Detection Zones")]
    [SerializeField]
    private bool _showShipCollisionDetectionZones = false;
    public bool ShowShipCollisionDetectionZones { get { return _showShipCollisionDetectionZones; } }

    [Tooltip("Show Fleet Formation Stations")]
    [SerializeField]
    private bool _showFleetFormationStations = false;
    public bool ShowFleetFormationStations { get { return _showFleetFormationStations; } }

    [Tooltip("Show Element Shields")]
    [SerializeField]
    private bool _showShields = false;
    public bool ShowShields { get { return _showShields; } }

    [Tooltip("Show Command Sensor Ranges")]
    [SerializeField]
    private bool _showSensors = false;
    public bool ShowSensors { get { return _showSensors; } }

    [Tooltip("Show Avoidable Obstacle Zones")]
    [SerializeField]
    private bool _showObstacleZones = false;
    public bool ShowObstacleZones { get { return _showObstacleZones; } }

    public override bool IsPersistentAcrossScenes { get { return true; } }  // GameScene -> GameScene retains values

    private string Name { get { return GetType().Name; } }

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        // TODO  
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        // TODO
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
    }

    private bool _showFleetCoursePlotsPrev = false;
    private void CheckShowFleetCoursePlots() {
        if (_showFleetCoursePlots != _showFleetCoursePlotsPrev) {
            D.Log("{0}.ShowFleetCoursePlots has changed from {1} to {2}.", Name, _showFleetCoursePlotsPrev, _showFleetCoursePlots);
            _showFleetCoursePlotsPrev = _showFleetCoursePlots;
            OnShowFleetCoursePlotsChanged();
        }
    }

    private bool _showShipCoursePlotsPrev = false;
    private void CheckShowShipCoursePlots() {
        if (_showShipCoursePlots != _showShipCoursePlotsPrev) {
            D.Log("{0}.ShowShipCoursePlots has changed from {1} to {2}.", Name, _showShipCoursePlotsPrev, _showShipCoursePlots);
            _showShipCoursePlotsPrev = _showShipCoursePlots;
            OnShowShipCoursePlotsChanged();
        }
    }

    private bool _showFleetVelocityRaysPrev = false;
    private void CheckShowFleetVelocityRays() {
        if (_showFleetVelocityRays != _showFleetVelocityRaysPrev) {
            D.Log("{0}.ShowFleetVelocityRays has changed from {1} to {2}.", Name, _showFleetVelocityRaysPrev, _showFleetVelocityRays);
            _showFleetVelocityRaysPrev = _showFleetVelocityRays;
            OnShowFleetVelocityRaysChanged();
        }
    }

    private bool _showShipVelocityRaysPrev = false;
    private void CheckShowShipVelocityRays() {
        if (_showShipVelocityRays != _showShipVelocityRaysPrev) {
            D.Log("{0}.ShowShipVelocityRays has changed from {1} to {2}.", Name, _showShipVelocityRaysPrev, _showShipVelocityRays);
            _showShipVelocityRaysPrev = _showShipVelocityRays;
            OnShowShipVelocityRaysChanged();
        }
    }

    private bool _showFleetFormationStationsPrev;
    private void CheckShowFleetFormationStations() {
        if (_showFleetFormationStations != _showFleetFormationStationsPrev) {
            D.Log("{0}.ShowShipFormationStations has changed from {1} to {2}.", Name, _showFleetFormationStationsPrev, _showFleetFormationStations);
            _showFleetFormationStationsPrev = _showFleetFormationStations;
            OnShowFormationStationsChanged();
        }
    }

    private bool _showShipCollisionDetectionZonesPrev;
    private void CheckShowShipCollisionDetectionZones() {
        if (_showShipCollisionDetectionZones != _showShipCollisionDetectionZonesPrev) {
            D.Log("{0}.ShowShipCollisionDetectionZones has changed from {1} to {2}.", Name, _showShipCollisionDetectionZonesPrev, _showShipCollisionDetectionZones);
            _showShipCollisionDetectionZonesPrev = _showShipCollisionDetectionZones;
            OnShowShipCollisionDetectionZonesChanged();
        }
    }

    private bool _showShieldsPrev;
    private void CheckShowShields() {
        if (_showShields != _showShieldsPrev) {
            D.Log("{0}.ShowShields has changed from {1} to {2}.", Name, _showShieldsPrev, _showShields);
            _showShieldsPrev = _showShields;
            OnShowShieldsChanged();
        }
    }

    private bool _showSensorsPrev;
    private void CheckShowSensors() {
        if (_showSensors != _showSensorsPrev) {
            D.Log("{0}.ShowSensors has changed from {1} to {2}.", Name, _showSensorsPrev, _showSensors);
            _showSensorsPrev = _showSensors;
            OnShowSensorsChanged();
        }
    }

    private bool _showObstacleZonesPrev;
    private void CheckShowObstacleZones() {
        if (_showObstacleZones != _showObstacleZonesPrev) {
            D.Log("{0}.ShowObstacleZones has changed from {1} to {2}.", Name, _showObstacleZonesPrev, _showObstacleZones);
            _showObstacleZonesPrev = _showObstacleZones;
            OnShowObstacleZonesChanged();
        }
    }

    #endregion

    #region Event and Prop Change Handlers

    private void OnShowFleetCoursePlotsChanged() {
        if (showFleetCoursePlotsChanged != null) {
            showFleetCoursePlotsChanged(Instance, new EventArgs());
        }
    }

    private void OnShowShipCoursePlotsChanged() {
        if (showShipCoursePlotsChanged != null) {
            showShipCoursePlotsChanged(Instance, new EventArgs());
        }
    }

    private void OnShowFleetVelocityRaysChanged() {
        if (showFleetVelocityRaysChanged != null) {
            showFleetVelocityRaysChanged(Instance, new EventArgs());
        }
    }

    private void OnShowShipVelocityRaysChanged() {
        if (showShipVelocityRaysChanged != null) {
            showShipVelocityRaysChanged(Instance, new EventArgs());
        }
    }

    private void OnShowFormationStationsChanged() {
        if (showFleetFormationStationsChanged != null) {
            showFleetFormationStationsChanged(Instance, new EventArgs());
        }
    }

    private void OnShowShipCollisionDetectionZonesChanged() {
        if (showShipCollisionDetectionZonesChanged != null) {
            showShipCollisionDetectionZonesChanged(Instance, new EventArgs());
        }
    }

    private void OnShowShieldsChanged() {
        if (showShieldsChanged != null) {
            showShieldsChanged(Instance, new EventArgs());
        }
    }

    private void OnShowSensorsChanged() {
        if (showSensorsChanged != null) {
            showSensorsChanged(Instance, new EventArgs());
        }
    }

    private void OnShowObstacleZonesChanged() {
        if (showObstacleZonesChanged != null) {
            showObstacleZonesChanged(Instance, new EventArgs());
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        // TODO
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

