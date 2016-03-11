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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one OrbitPeriod.
    /// </summary>
    [SerializeField]
    private float _relativeOrbitRate = 1.0F;

    private bool _isActivated;
    /// <summary>
    /// Control for activating this OrbitSimulator. Activating the simulator does not necessarily
    /// cause the simulator to rotate as it may be set by the OrbitSlot to not rotate.
    /// </summary>
    public bool IsActivated {
        get { return _isActivated; }
        set { SetProperty<bool>(ref _isActivated, value, "IsActivated", IsActivatedPropChangedHandler); }
    }

    private AOrbitSlot _orbitSlot;
    public AOrbitSlot OrbitSlot {
        protected get { return _orbitSlot; }
        set {
            D.Assert(_orbitSlot == null);   // one time only
            _orbitSlot = value;
            OrbitSlotPropSetHandler();
        }
    }

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    protected Vector3 _axisOfOrbit = Vector3.up;

    /// <summary>
    /// The rate this OrbitSimulator orbits around the orbited object in degrees per hour.
    /// </summary>
    protected float _orbitRateInDegreesPerHour;
    protected GameTime _gameTime;

    /// <summary>
    /// The speed of the orbiting object around the orbited object in units per hour. 
    /// This value will increase as the radius of the orbit increases.
    /// </summary>
    private float _orbitSpeedInUnitsPerHour;
    private IList<IDisposable> _subscriptions;
    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = References.GameManager;
        _gameTime = GameTime.Instance;
        UpdateRate = FrameUpdateFrequency.Frequent;
        Subscribe();
        enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    /// <summary>
    /// Acquires the speed at which the body located at <c>radius</c> units from the orbit center is traveling 
    /// in Units per hour. This value is always relative to the body being orbited.
    /// e.g. the speed of a planet around a system is relative to an unmoving system, so this value
    /// is the speed the planet is traveling in the universe. Conversely, the speed of a moon around a planet
    /// is relative to the moving planet, so the value returned for the moon does not account for the 
    /// speed of the planet.
    /// </summary>
    /// <param name="radius">The distance from the center of the orbited body to the body that is orbiting.</param>
    /// <returns></returns>
    public float GetRelativeOrbitSpeed(float radius) {
        if (_orbitSpeedInUnitsPerHour == Constants.ZeroF) {
            _orbitSpeedInUnitsPerHour = (2F * Mathf.PI * radius) / (OrbitSlot.OrbitPeriod.TotalInHours / _relativeOrbitRate);
        }
        return _orbitSpeedInUnitsPerHour;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTimeSinceLastUpdate = _gameTime.DeltaTimeOrPaused * (int)UpdateRate;
        //D.Log("Time.DeltaTime = {0}, GameTime.DeltaTimeWithGameSpeed = {1}, UpdateRate = {2}.", Time.deltaTime, GameTime.DeltaTimeOrPausedWithGameSpeed, (int)UpdateRate);
        UpdateOrbit(deltaTimeSinceLastUpdate);
    }

    /// <summary>
    /// Updates the rotation of this object around its axis of orbit (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited. The visual speed of the orbit varies with game speed.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The delta time (zero if paused) since last update.</param>
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

    private void OrbitSlotPropSetHandler() {
        _orbitRateInDegreesPerHour = _relativeOrbitRate * Constants.DegreesPerOrbit / (float)OrbitSlot.OrbitPeriod.TotalInHours;
    }

    #endregion

    private void AssessEnabled() {
        enabled = OrbitSlot.ToOrbit && IsActivated && !_gameMgr.IsPaused;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

