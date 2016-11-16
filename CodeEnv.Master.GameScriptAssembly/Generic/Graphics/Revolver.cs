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

    private const int UpdateRotationCounterThreshold = 4;

    [SerializeField]
    private bool _rotateDuringPause = true;
    public bool RotateDuringPause { get { return _rotateDuringPause; } }

    /// <summary>
    /// The axis of self rotation in local space.
    /// </summary>
    [SerializeField]
    private Vector3 _axisOfRotation = Vector3.up;

    /// <summary>
    /// The self rotation rate of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    [SerializeField]
    private float _relativeRotationRate = 0.1F;

    private bool _isActivated;
    /// <summary>
    /// Control for activating this Revolver. Activating does not necessarily
    /// cause the revolver to rotate as it may be set to not rotate during a pause.
    /// </summary>
    public bool IsActivated {
        get { return _isActivated; }
        set { SetProperty<bool>(ref _isActivated, value, "IsActivated", IsActivatedPropChangedHandler); }
    }

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    private GameTimeDuration _rotationPeriod; // IMPROVE use custom editor to make settable from inspector

    /// <summary>
    /// The rotation rate of the object around <c>axisOfRotation</c> in degrees per hour.
    /// </summary>
    private float _rotationRate;
    private GameTime _gameTime;
    private IList<IDisposable> _subscriptions;
    private IGameManager _gameMgr;
    private int _updateRotationCounter;

    protected override void Awake() {
        base.Awake();
        _gameTime = GameTime.Instance;
        _gameMgr = References.GameManager;
        _rotationPeriod = GameTimeDuration.OneDay;
        _rotationRate = _relativeRotationRate * Constants.DegreesPerRotation / (float)_rotationPeriod.TotalInHours;
        Subscribe();
        enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsRunning, IsRunningPropChangedHandler));
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    // Note: Revolvers no longer control their own enabled state based on visibility as DisplayManagers also need to control it based on IntelCoverage

    void Update() {
        if (_updateRotationCounter >= UpdateRotationCounterThreshold) {
            float deltaTimeSinceLastUpdate = _gameTime.DeltaTime * _updateRotationCounter;
            UpdateRotation(deltaTimeSinceLastUpdate);
            _updateRotationCounter = Constants.Zero;
            return;
        }
        _updateRotationCounter++;
    }

    /// <summary>
    /// Updates the rotation of the revolving object this script is attached to around 
    /// the <c>axisOfRotation</c>. For aesthetic purposes, the visual rate of rotation 
    /// varies with gameSpeed and it may not cease while paused.
    /// OPTIMIZE Consider calling this centrally for all revolvers.
    /// </summary>
    /// <param name="deltaTimeSinceLastUpdate">The speed adjusted elapsed time since the last update.</param>
    private void UpdateRotation(float deltaTimeSinceLastUpdate) {
        var degreesToRotate = _rotationRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTimeSinceLastUpdate;
        transform.Rotate(_axisOfRotation, degreesToRotate, relativeTo: Space.Self);
    }

    #region Event and Property Change Handlers

    private void IsRunningPropChangedHandler() {
        if (!_gameMgr.IsRunning) {
            enabled = false;    // stop accessing GameTime once GameInstance is no longer running
        }
    }

    private void IsActivatedPropChangedHandler() {
        D.Assert(_gameMgr.IsRunning);
        AssessEnabled();
    }

    private void IsPausedPropChangedHandler() {
        AssessEnabled();
    }

    #endregion

    private void AssessEnabled() {
        enabled = IsActivated && (!_gameMgr.IsPaused || RotateDuringPause);
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

