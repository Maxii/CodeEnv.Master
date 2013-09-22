// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Orbit.cs
// Class that simulates the movement of an object orbiting around a stationary location. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of an object orbiting around a stationary
/// location. Both orbital movement and self rotation of the orbiting object are implemented.
/// Assumes this script is attached to a parent of [rotatingObject] whose position is coincident
/// with that of the stationary object it is orbiting. This script simulates
/// orbital movement of [rotatingObject] by rotating this parent object.
/// </summary>
public class Orbit : AMonoBehaviourBase {

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
    public GameTimePeriod orbitPeriod;

    /// <summary>
    /// The axis of self rotation in local space.
    /// </summary>
    public Vector3 axisOfRotation = Vector3.up;

    /// <summary>
    /// The self rotation speed of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    public float relativeRotationSpeed = 1.0F;

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    public GameTimePeriod rotationPeriod;

    /// <summary>
    /// The object that is to self rotate at [rotationSpeed]. Can be null if the object is not
    /// to self rotate.
    /// </summary>
    public Transform rotatingObject;

    /// <summary>
    /// The orbit speed of the object around the stationary location in degrees per second.
    /// </summary>
    protected float _orbitSpeed;

    /// <summary>
    /// The self rotation speed of the object around its own axis in degrees per second.
    /// </summary>
    protected float _rotationSpeed;

    protected float _gameSpeedMultiplier;

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Continuous;
        orbitPeriod = orbitPeriod ?? new GameTimePeriod(days: 0, years: 1);
        rotationPeriod = rotationPeriod ?? new GameTimePeriod(days: 10, years: 0);
        _orbitSpeed = (relativeOrbitSpeed * Constants.DegreesPerOrbit * GeneralSettings.Instance.DaysPerSecond) / orbitPeriod.PeriodInDays;
        _rotationSpeed = (relativeRotationSpeed * Constants.DegreesPerRotation * GeneralSettings.Instance.DaysPerSecond) / rotationPeriod.PeriodInDays;
    }

    void Update() {
        if (ToUpdate()) {
            OnUpdate();
        }
    }

    protected virtual void OnUpdate() {
        float adjustedDeltaTime = GameTime.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;
        // rotates this parent object (coincident with the position of the location to orbit) around its 
        // LOCAL Y axis to simulate an orbit around the object to orbit
        _transform.Rotate(axisOfOrbit * _orbitSpeed * adjustedDeltaTime, relativeTo: Space.Self);

        if (rotatingObject != null) {
            // rotates the child object around its own LOCAL Y axis
            rotatingObject.Rotate(axisOfRotation * _rotationSpeed * adjustedDeltaTime, relativeTo: Space.Self);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

