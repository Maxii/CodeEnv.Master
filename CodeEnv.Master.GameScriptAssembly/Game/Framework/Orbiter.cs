// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Orbiter.cs
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
/// Class that simulates the movement of an object orbiting around an immobile location. 
/// Assumes this script is attached to an otherwise empty gameobject [the orbiterGO] whose parent is the object
/// being orbited. The position of this orbiterGO should be coincident with that of the object being orbited. The
/// object that is orbiting is parented to this orbiterGO, thus simulating orbital movement by 
/// changing the rotation of the orbiterGO.
/// </summary>
public class Orbiter : AMonoBase, IOrbiter {

    private bool _isOrbiterInMotion;
    public bool IsOrbiterInMotion {
        get { return _isOrbiterInMotion; }
        set { SetProperty<bool>(ref _isOrbiterInMotion, value, "IsOrbiterInMotion", OnIsOrbiterInMotionChanged); }
    }

    /// <summary>
    /// The duration of one orbit of the object around the location.
    /// </summary>
    public GameTimeDuration OrbitPeriod { get; set; }

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    public Vector3 axisOfOrbit = Vector3.up;

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one Year.
    /// </summary>
    public float relativeOrbitSpeed = 1.0F;

    /// <summary>
    /// The speed of the orbiting object around the orbited object in degrees per second.
    /// </summary>
    protected float _orbitSpeedInDegreesPerSecond;

    /// <summary>
    /// The speed of the orbiting object around the orbited object in units per hour
    /// </summary>
    private float _orbitSpeedInUnitsPerHour;
    private IList<IDisposable> _subscribers;
    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = References.GameManager;
        UpdateRate = FrameUpdateFrequency.Frequent;
        Subscribe();
        enabled = false;
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, OnIsPausedChanged));
    }

    protected override void Start() {
        base.Start();
        D.Assert(OrbitPeriod != default(GameTimeDuration), "{0}.{1}.OrbitPeriod has not been set.".Inject(_transform.name, GetType().Name));
        _orbitSpeedInDegreesPerSecond = relativeOrbitSpeed * Constants.DegreesPerOrbit * (GameTime.HoursPerSecond / (float)OrbitPeriod.TotalInHours);
        //D.Log("OrbitSpeedInDegreesPerSecond = {0}, OrbitPeriodInTotalHours = {1}.", _orbitSpeedInDegreesPerSecond, orbitPeriod.TotalInHours);
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.Instance.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;
        //D.Log("Time.DeltaTime = {0}, GameTime.DeltaTimeWithGameSpeed = {1}, UpdateRate = {2}.", Time.deltaTime, GameTime.DeltaTimeOrPausedWithGameSpeed, (int)UpdateRate);
        UpdateOrbit(deltaTime);
    }

    /// <summary>
    /// Updates the rotation of this object around its local Y axis (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected virtual void UpdateOrbit(float deltaTime) {
        float desiredStepAngle = _orbitSpeedInDegreesPerSecond * deltaTime;
        _transform.Rotate(axisOfOrbit, desiredStepAngle, relativeTo: Space.Self);
        //_transform.Rotate(axisOfOrbit * _orbitSpeedInDegreesPerSecond * deltaTime, relativeTo: Space.Self);
        //_transform.Rotate(0F, desiredStepAngle, 0F, relativeTo: Space.Self);
    }

    private void OnIsOrbiterInMotionChanged() {
        AssessOrbiterInMotion();
    }

    private void OnIsPausedChanged() {
        AssessOrbiterInMotion();
    }

    private void AssessOrbiterInMotion() {
        enabled = IsOrbiterInMotion && !_gameMgr.IsPaused;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IOrbiter Members

    public Transform Transform { get { return _transform; } }

    /// <summary>
    /// Acquires the speed at which the body located at <c>radius</c> units
    /// from the orbit center is traveling. This value is always relative to the body being orbited.
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

