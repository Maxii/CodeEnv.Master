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
    /// The duration of one orbit of the object around the location.
    /// </summary>
    private GameTimeDuration _orbitPeriod; // IMPROVE use custom editor to make setable from inspector

    /// <summary>
    /// The speed of the orbiting object around the orbited object in degrees per second.
    /// </summary>
    protected float _orbitSpeedInDegreesPerSecond;

    /// <summary>
    /// The speed of the orbiting object around the orbited object in units per hour
    /// </summary>
    private float _orbitSpeedInUnitsPerHour;

    private GameStatus _gameStatus;

    protected override void Awake() {
        base.Awake();
        _gameStatus = GameStatus.Instance;
        _orbitPeriod = GameTimeDuration.OneYear;
        _orbitSpeedInDegreesPerSecond = relativeOrbitSpeed * Constants.DegreesPerOrbit * (GameTime.HoursPerSecond / (float)_orbitPeriod.TotalInHours);
        UpdateRate = FrameUpdateFrequency.Frequent;
        enabled = false;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;    // stops the orbit when paused
        if (!_gameStatus.IsPaused) {
            UpdateOrbit(deltaTime);
        }
    }

    /// <summary>
    /// Updates the rotation of this object around its local Y axis (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected virtual void UpdateOrbit(float deltaTime) {
        _transform.Rotate(axisOfOrbit * _orbitSpeedInDegreesPerSecond * deltaTime, relativeTo: Space.Self);
    }

    /// <summary>
    /// Acquires the speed at which the body located at <c>radius</c> units
    /// from the orbit center is traveling.
    /// </summary>
    /// <param name="radius">The distance from the center of the orbited body to the body that is orbiting.</param>
    /// <returns></returns>
    public float GetSpeedOfBodyInOrbit(float radius) {
        if (_orbitSpeedInUnitsPerHour == Constants.ZeroF) {
            _orbitSpeedInUnitsPerHour = (2F * Mathf.PI * radius) / (_orbitPeriod.TotalInHours / relativeOrbitSpeed);
        }
        return _orbitSpeedInUnitsPerHour;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

