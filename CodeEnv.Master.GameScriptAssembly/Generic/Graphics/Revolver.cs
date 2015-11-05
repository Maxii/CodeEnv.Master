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
    /// The self rotation rate of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    public float relativeRotationRate = 0.1F;

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    private GameTimeDuration _rotationPeriod; // IMPROVE use custom editor to make setable from inspector

    /// <summary>
    /// The rotation rate of the object around <c>axisOfRotation</c> in degrees per hour.
    /// </summary>
    private float _rotationRate;
    private GameTime _gameTime;
    private IList<IDisposable> _subscriptions;

    protected override void Awake() {
        base.Awake();
        _gameTime = GameTime.Instance;
        _rotationPeriod = GameTimeDuration.OneDay;
        _rotationRate = relativeRotationRate * Constants.DegreesPerRotation / (float)_rotationPeriod.TotalInHours;
        UpdateRate = FrameUpdateFrequency.Frequent;
        Subscribe();
        enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(References.GameManager.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsRunning, OnIsRunningChanged));
    }

    // Note: Revolvers no longer control their own enabled state based on visibility as DisplayManagers also need to control it based on IntelCoverage

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTimeSinceLastUpdate = _gameTime.DeltaTime * (int)UpdateRate;
        UpdateRotation(deltaTimeSinceLastUpdate);
    }

    /// <summary>
    /// Updates the rotation of the revolving object this script is attached to around 
    /// the <c>axisOfRotation</c>. For esthetic purposes, the visual rate of rotation 
    /// varies with gameSpeed and it does not cease while paused.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The speed adjusted elapsed time since the last update.</param>
    private void UpdateRotation(float deltaTimeSinceLastUpdate) {
        var degreesToRotate = _rotationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.Rotate(axisOfRotation, degreesToRotate, relativeTo: Space.Self);
    }

    private void OnIsRunningChanged() {
        if (!References.GameManager.IsRunning) {
            enabled = false;    // stop accessing GameTime once GameInstance is no longer running
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

