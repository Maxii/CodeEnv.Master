// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Revolve.cs
// Class that rotates an object around a designated axis.
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
/// Class that rotates an object around a designated axis.
/// </summary>
public class Revolve : AMonoBase {

    /// <summary>
    /// The axis of self rotation in local space.
    /// </summary>
    public Vector3 axisOfRotation = Vector3.up;

    /// <summary>
    /// The self rotation speed of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    public float relativeRotationSpeed = 0.1F;

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    private GameTimeDuration _rotationPeriod; // IMPROVE use custom editor to make setable from inspector

    /// <summary>
    /// The self rotation speed of the object around its own axis in degrees per second.
    /// </summary>
    private float _rotationSpeed;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<MeshRenderer>(gameObject);
        _rotationPeriod = GameTimeDuration.OneDay;
        _rotationSpeed = relativeRotationSpeed * Constants.DegreesPerRotation * (GameTime.HoursPerSecond / (float)_rotationPeriod.TotalInHours);
        UpdateRate = FrameUpdateFrequency.Frequent;
        enabled = false;
    }

    void OnBecameVisible() {
        enabled = true;
        //D.Log("{0}.enabled.", _transform.name);
    }

    void OnBecameInvisible() {
        enabled = false;
        //D.Log("{0}.disabled.", _transform.name);
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.DeltaTimeWithGameSpeed * (int)UpdateRate;    // Rotates when paused
        UpdateRotation(deltaTime);
    }

    /// <summary>
    /// Updates the rotation of 'rotatingObject' around its own local Y axis.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    private void UpdateRotation(float deltaTime) {
        _transform.Rotate(axisOfRotation * _rotationSpeed * deltaTime, relativeTo: Space.Self);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

