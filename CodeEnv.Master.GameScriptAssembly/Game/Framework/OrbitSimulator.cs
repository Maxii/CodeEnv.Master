// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitSimulator.cs
// Class that simulates the movement of an object orbiting around a stationary location. 
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
/// Class that simulates the movement of an object orbiting around a stationary location. 
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the object
/// being orbited. The position of this orbiterGO should be coincident with that of the object being orbited. The
/// object that is orbiting can either be parented to this orbiterGO or tied to it with a fixed joint,
/// thus simulating orbital movement by rotating the orbiterGO.
/// </summary>
public class OrbitSimulator : AMonoBase, IOrbitSimulator {

    private bool _isActive;
    /// <summary>
    /// Flag indicating whether the OrbitSimulator is orbiting (moving) around its orbited object.
    /// </summary>
    public bool IsActive {
        get { return _isActive; }
        set { SetProperty<bool>(ref _isActive, value, "IsActive", OnIsActiveChanged); }
    }

    /// <summary>
    /// The duration of one 360 degree orbit around the orbited object.
    /// </summary>
    public GameTimeDuration OrbitPeriod { get; set; }

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    public Vector3 axisOfOrbit = Vector3.up;

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one OrbitPeriod.
    /// </summary>
    public float relativeOrbitSpeed = 1.0F;

    /// <summary>
    /// The speed of this OrbitSimulator around the orbited object in degrees per second.
    /// </summary>
    protected float _orbitSpeedInDegreesPerSecond;

    /// <summary>
    /// The speed of the orbiting object around the orbited object in units per hour. 
    /// This value will increase as the radius of the orbit increases.
    /// </summary>
    private float _orbitSpeedInUnitsPerHour;
    private IList<IDisposable> _subscriptions;
    private IGameManager _gameMgr;
    private GameTime _gameTime;

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
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, OnIsPausedChanged));
    }

    protected override void Start() {
        base.Start();
        D.Assert(OrbitPeriod != default(GameTimeDuration), "{0}.{1}.OrbitPeriod has not been set.".Inject(_transform.name, GetType().Name));
        var orbitSpeedInDegreesPerHour = relativeOrbitSpeed * Constants.DegreesPerOrbit / (float)OrbitPeriod.TotalInHours;
        _orbitSpeedInDegreesPerSecond = orbitSpeedInDegreesPerHour * GameTime.HoursPerSecond;
        //D.Log("OrbitSpeedInDegreesPerSecond = {0}, OrbitPeriodInTotalHours = {1}.", _orbitSpeedInDegreesPerSecond, orbitPeriod.TotalInHours);
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float gameSpeedAdjustedDeltaTimeSinceLastUpdate = _gameTime.GameSpeedAdjustedDeltaTimeOrPaused * (int)UpdateRate;
        //D.Log("Time.DeltaTime = {0}, GameTime.DeltaTimeWithGameSpeed = {1}, UpdateRate = {2}.", Time.deltaTime, GameTime.DeltaTimeOrPausedWithGameSpeed, (int)UpdateRate);
        UpdateOrbit(gameSpeedAdjustedDeltaTimeSinceLastUpdate);
    }

    /// <summary>
    /// Updates the rotation of this object around its local Y axis (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited. The delta time used is adjusted for GameSpeed
    /// so the orbit rotates faster or slower depending on the GameSpeed.
    /// </summary>
    /// <param name="gameSpeedAdjustedDeltaTime">The delta time adjusted for GameSpeed.</param>
    protected virtual void UpdateOrbit(float gameSpeedAdjustedDeltaTime) {
        float degreesToRotate = _orbitSpeedInDegreesPerSecond * gameSpeedAdjustedDeltaTime;
        _transform.Rotate(axisOfOrbit, degreesToRotate, relativeTo: Space.Self);
    }

    private void OnIsActiveChanged() {
        AssessEnabled();
    }

    private void OnIsPausedChanged() {
        AssessEnabled();
    }

    private void AssessEnabled() {
        enabled = IsActive && !_gameMgr.IsPaused;
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

    #region IOrbitSimulator Members

    public Transform Transform { get { return _transform; } }

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
            _orbitSpeedInUnitsPerHour = (2F * Mathf.PI * radius) / (OrbitPeriod.TotalInHours / relativeOrbitSpeed);
        }
        return _orbitSpeedInUnitsPerHour;
    }

    #endregion

}

