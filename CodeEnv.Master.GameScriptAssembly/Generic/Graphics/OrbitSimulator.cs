// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitSimulator.cs
// Simulates orbiting around an immobile parent of any children of the simulator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Simulates orbiting around an immobile parent of any children of the simulator.
/// </summary>
public class OrbitSimulator : AMonoBase, IOrbitSimulator {

    private const int UpdateOrbitCounterThreshold = 4;

    protected const string DebugNameFormat = "{0}.{1}";

    private string _debugName;
    public virtual string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(OrbitData.OrbitedItem.name, GetType().Name);
            }
            return _debugName;
        }
    }

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one OrbitPeriod.
    /// </summary>
    [SerializeField]
    private float _relativeOrbitRate = 1.0F;

    private bool _isActivated;
    /// <summary>
    /// Control for activating this OrbitSimulator. Activating the simulator does not necessarily
    /// cause the simulator to rotate as it may be set by the OrbitData to not rotate.
    /// <remarks>This has nothing to do with the active property of a GameObject.</remarks>
    /// </summary>
    public bool IsActivated {
        get { return _isActivated; }
        set { SetProperty<bool>(ref _isActivated, value, "IsActivated", IsActivatedPropChangedHandler); }
    }

    private Rigidbody _orbitRigidbody;
    public Rigidbody OrbitRigidbody {
        get {
            if (_orbitRigidbody == null) {
                _orbitRigidbody = gameObject.AddMissingComponent<Rigidbody>();
                _orbitRigidbody.useGravity = false;
                _orbitRigidbody.isKinematic = true;
            }
            return _orbitRigidbody;
        }
    }

    private OrbitData _orbitData;
    public OrbitData OrbitData {
        get { return _orbitData; }
        set {
            D.AssertNull(_orbitData);   // one time only
            _orbitData = value;
            OrbitDataPropSetHandler();
        }
    }

    /// <summary>
    /// The speed of travel in units per hour of the OrbitingItem located at a radius of OrbitData.MeanRadius
    /// from the OrbitedItem. This value is always relative to the body being orbited.
    /// <remarks>The speed of a planet around a system is relative to an unmoving system, so this value
    /// is the speed the planet is traveling in the universe. Conversely, the speed of a moon around a planet
    /// is relative to the moving planet, so the value returned for the moon does not account for the 
    /// speed of the planet.</remarks>
    /// </summary>
    public float RelativeOrbitSpeed { get; private set; }

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    protected Vector3 _axisOfOrbit = Vector3.up;

    /// <summary>
    /// The rate this OrbitSimulator orbits around the orbited object in degrees per hour.
    /// </summary>
    protected float _orbitRateInDegreesPerHour;
    protected GameTime _gameTime;

    private IList<IDisposable> _subscriptions;
    private IGameManager _gameMgr;
    private int _updateOrbitCounter;

    protected override void Awake() {
        base.Awake();
        _gameMgr = References.GameManager;
        _gameTime = GameTime.Instance;
        Subscribe();
        enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    private float InitializeOrbitSpeed() {
        float orbitSpeedInUnitsPerHour = (2F * Mathf.PI * OrbitData.MeanRadius) / (OrbitData.OrbitPeriod.TotalInHours / _relativeOrbitRate);
        if (!(this is ShipCloseOrbitSimulator)) {
            if (orbitSpeedInUnitsPerHour > TempGameValues.__MaxPlanetoidOrbitSpeed) {
                D.Warn("{0} orbitSpeed {1:0.0000} > max {2:0.0000}.", DebugName, orbitSpeedInUnitsPerHour, TempGameValues.__MaxPlanetoidOrbitSpeed);
            }
        }
        return orbitSpeedInUnitsPerHour;
    }

    void Update() {
        if (_updateOrbitCounter >= UpdateOrbitCounterThreshold) {
            float deltaTimeSinceLastUpdate = _gameTime.DeltaTime * _updateOrbitCounter;
            UpdateOrbit(deltaTimeSinceLastUpdate);
            _updateOrbitCounter = Constants.Zero;
            return;
        }
        _updateOrbitCounter++;
    }

    /// <summary>
    /// Updates the rotation of this object around its axis of orbit (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited. The visual speed of the orbit varies with game speed.
    /// OPTIMIZE Consider calling this centrally every x updates.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time since last update.</param>
    protected virtual void UpdateOrbit(float deltaTimeSinceLastUpdate) {
        float degreesToRotate = _orbitRateInDegreesPerHour * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.Rotate(_axisOfOrbit, degreesToRotate, relativeTo: Space.Self);
    }

    #region Event and Property Change Handlers

    private void IsActivatedPropChangedHandler() {
        D.Assert(_gameMgr.IsRunning);
        AssessEnabled();
    }

    private void IsPausedPropChangedHandler() {
        AssessEnabled();
    }

    private void OrbitDataPropSetHandler() {
        _orbitRateInDegreesPerHour = _relativeOrbitRate * Constants.DegreesPerOrbit / (float)OrbitData.OrbitPeriod.TotalInHours;
        RelativeOrbitSpeed = InitializeOrbitSpeed();
    }

    #endregion

    private void AssessEnabled() {
        enabled = OrbitData.ToOrbit && IsActivated && !_gameMgr.IsPaused;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return DebugName;
    }


}

