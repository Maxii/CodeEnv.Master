// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Revolver.cs
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
public class Revolver : AMonoBase, IRevolver {

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
    /// The rotation speed of the object around <c>axisOfRotation</c> in degrees per second.
    /// </summary>
    private float _rotationSpeed;
    private GameTime _gameTime;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<MeshRenderer>(gameObject);
        _gameTime = GameTime.Instance;
        _rotationPeriod = GameTimeDuration.OneDay;
        var rotationSpeedInDegreesPerHour = relativeRotationSpeed * Constants.DegreesPerRotation / (float)_rotationPeriod.TotalInHours;
        _rotationSpeed = rotationSpeedInDegreesPerHour * GameTime.HoursPerSecond;
        UpdateRate = FrameUpdateFrequency.Frequent;
        enabled = false;
    }

    // Note: Revolvers no longer control their own enabled state based on visibility as I also need to control it based on IntelCoverage

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float gameSpeedAdjustedDeltaTimeSinceLastUpdate = _gameTime.GameSpeedAdjustedDeltaTime * (int)UpdateRate;
        UpdateRotation(gameSpeedAdjustedDeltaTimeSinceLastUpdate);
    }

    /// <summary>
    /// Updates the rotation of the revolving object this script is attached to around 
    /// the <c>axisOfRotation</c>. Using <c>deltaTimeInGameSecs</c> speeds up or
    /// slows down the rotation rate based on GameSpeed - aka rotates faster at higher
    /// GameSpeeds. For esthetic purposes, rotation does not cease while paused.
    /// </summary>
    /// <param name="speedAdjustedDeltaTimeSinceLastUpdate">The speed adjusted elapsed time since the last update.</param>
    private void UpdateRotation(float speedAdjustedDeltaTimeSinceLastUpdate) {
        var degreesToRotate = _rotationSpeed * speedAdjustedDeltaTimeSinceLastUpdate;
        _transform.Rotate(axisOfRotation, degreesToRotate, relativeTo: Space.Self);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

