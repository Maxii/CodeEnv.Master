﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CollisionDetectionMonitor.cs
// Detects ObstacleZoneColliders entering and exiting this ship's collision detection zone.
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
/// Detects ObstacleZoneColliders entering and exiting this ship's collision detection zone.
/// </summary>
public class CollisionDetectionMonitor : AColliderMonitor {

    private const string NameFormat = "{0}.{1}";

    public override string FullName {
        get {
            if (ParentItem == null) { return base.FullName; }
            return NameFormat.Inject(ParentItem.FullName, GetType().Name);
        }
    }

    public new IShip ParentItem {
        get { return base.ParentItem as IShip; }
        set { base.ParentItem = value as IShip; }
    }

    protected override bool IsTriggerCollider { get { return true; } }

    /// <summary>
    /// Flag indicating whether a Kinematic Rigidbody is required by the monitor.
    /// <remarks>Must have a rigidbody in order to fire trigger events as all other ObstacleZone Colliders are static.
    /// </remarks>
    /// </summary>
    protected override bool IsKinematicRigidbodyReqd { get { return true; } }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _collider.gameObject.layer = (int)Layers.CollisionDetectionZone;
        InitializeDebugShowCollisionDetectionZone();
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when [trigger enter].
    /// </summary>
    /// <param name="obstacleZoneCollider">The obstacle zone collider that we have encountered. 
    /// Can also be another ship's collision detection collider.</param>
    protected override void OnTriggerEnter(Collider obstacleZoneCollider) {
        base.OnTriggerEnter(obstacleZoneCollider);
        if (obstacleZoneCollider == _collider) {
            D.Warn("{0} entering its own CollisionDetectionCollider?!", FullName);
            return;
        }
        IObstacle obstacle = obstacleZoneCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
        if (_gameMgr.IsPaused) {
            D.Warn("{0}.OnTriggerEnter() tripped by {1} while paused.", FullName, obstacle.FullName);
            RecordObstacleEnteringWhilePaused(obstacle);
            return;
        }
        ParentItem.HandlePendingCollisionWith(obstacle);
    }

    /// <summary>
    /// Called when [trigger exit].
    /// </summary>
    /// <param name="obstacleZoneCollider">The obstacle zone collider that we have encountered. 
    /// Can also be another ship's collision detection collider.</param>
    protected override void OnTriggerExit(Collider obstacleZoneCollider) {
        base.OnTriggerExit(obstacleZoneCollider);
        if (obstacleZoneCollider == _collider) {
            D.Warn("{0} exiting its own CollisionDetectionCollider?!", FullName);
            return;
        }
        IObstacle obstacle = obstacleZoneCollider.gameObject.GetSafeFirstInterfaceInParents<IObstacle>(excludeSelf: true);
        if (_gameMgr.IsPaused) {
            D.Warn("{0}.OnTriggerExit() tripped by {1} while paused.", FullName, obstacle.FullName);
            RecordObstacleExitingWhilePaused(obstacle);
            return;
        }
        ParentItem.HandlePendingCollisionAverted(obstacle);
    }

    protected override void IsOperationalPropChangedHandler() { }

    protected override void ParentItemPropSetHandler() {
        base.ParentItemPropSetHandler();
        RangeDistance = ParentItem.Radius * 2F;
        D.Warn(RangeDistance > TempGameValues.LargestShipCollisionDetectionZoneRadius, "{0}: CollisionDetectionZoneRadius {1:0.##} > {2:0.##}.",
            FullName, RangeDistance, TempGameValues.LargestShipCollisionDetectionZoneRadius);
    }

    protected override void IsPausedPropChangedHandler() {
        base.IsPausedPropChangedHandler();
        if (!_gameMgr.IsPaused) {
            HandleObstaclesEncounteredWhilePaused();
        }
    }

    #endregion

    #region Obstacles Encountered While Paused Handling System

    private IList<IObstacle> _enteringObstaclesEncounteredWhilePaused;
    private IList<IObstacle> _exitingObstaclesEncounteredWhilePaused;

    private void RecordObstacleEnteringWhilePaused(IObstacle obstacle) {
        if (_enteringObstaclesEncounteredWhilePaused == null) {
            _enteringObstaclesEncounteredWhilePaused = new List<IObstacle>();
        }
        if (CheckForPreviousPausedExitOf(obstacle)) {
            // while paused, previously exited and now entered so record to take action when unpaused
            D.Warn("{0} removing entering obstacle {1} already recorded as exited while paused.", FullName, obstacle.FullName);
            _exitingObstaclesEncounteredWhilePaused.Remove(obstacle);
        }
        _enteringObstaclesEncounteredWhilePaused.Add(obstacle);
    }

    private void RecordObstacleExitingWhilePaused(IObstacle obstacle) {
        if (_exitingObstaclesEncounteredWhilePaused == null) {
            _exitingObstaclesEncounteredWhilePaused = new List<IObstacle>();
        }
        if (CheckForPreviousPausedEntryOf(obstacle)) {
            // while paused, previously entered and now exited so eliminate record as no action should be taken when unpaused
            D.Warn("{0} removing exiting obstacle {1} already recorded as entered while paused.", FullName, obstacle.FullName);
            _enteringObstaclesEncounteredWhilePaused.Remove(obstacle);
            return;
        }
        _exitingObstaclesEncounteredWhilePaused.Add(obstacle);
    }

    private void HandleObstaclesEncounteredWhilePaused() {
        //D.Log(ShowDebugLog, "{0} handling obstacles encountered while paused, if any.", Name);
        __ValidateObstaclesEncounteredWhilePaused();
        if (!_enteringObstaclesEncounteredWhilePaused.IsNullOrEmpty()) {
            _enteringObstaclesEncounteredWhilePaused.ForAll(obs => ParentItem.HandlePendingCollisionWith(obs));
            _enteringObstaclesEncounteredWhilePaused.Clear();
        }
        if (!_exitingObstaclesEncounteredWhilePaused.IsNullOrEmpty()) {
            _exitingObstaclesEncounteredWhilePaused.ForAll(obs => ParentItem.HandlePendingCollisionAverted(obs));
            _exitingObstaclesEncounteredWhilePaused.Clear();
        }
    }

    private void __ValidateObstaclesEncounteredWhilePaused() {
        // there should be no obstacles that are present in both lists
        if (_enteringObstaclesEncounteredWhilePaused.IsNullOrEmpty() || _exitingObstaclesEncounteredWhilePaused.IsNullOrEmpty()) {
            return;
        }
        D.Assert(!_enteringObstaclesEncounteredWhilePaused.EqualsAnyOf(_exitingObstaclesEncounteredWhilePaused));
    }

    /// <summary>
    /// Returns <c>true</c> if the provided enteringObstacle has also been
    /// recorded as having exited while paused, <c>false</c> otherwise.
    /// </summary>
    /// <param name="obstacle">The enteringObstacle.</param>
    /// <returns></returns>
    private bool CheckForPreviousPausedExitOf(IObstacle enteringObstacle) {
        if (_exitingObstaclesEncounteredWhilePaused.IsNullOrEmpty()) {
            return false;
        }
        return _exitingObstaclesEncounteredWhilePaused.Contains(enteringObstacle);
    }

    /// <summary>
    /// Returns <c>true</c> if the provided exitingObstacle has also been
    /// recorded as having entered while paused, <c>false</c> otherwise.
    /// </summary>
    /// <param name="obstacle">The exitingObstacle.</param>
    /// <returns></returns>
    private bool CheckForPreviousPausedEntryOf(IObstacle exitingObstacle) {
        if (_enteringObstaclesEncounteredWhilePaused.IsNullOrEmpty()) {
            return false;
        }
        return _enteringObstaclesEncounteredWhilePaused.Contains(exitingObstacle);
    }

    #endregion

    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        D.Error("{0} does not support reuse.", FullName);
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupDebugShowCollisionDetectionZone();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Collision Detection Zone

    private void InitializeDebugShowCollisionDetectionZone() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showShipCollisionDetectionZonesChanged += ShowDebugCollisionDetectionZonesChangedEventHandler;
        if (debugValues.ShowShipCollisionDetectionZones) {
            EnableDebugShowCollisionDetectionZone(true);
        }
    }

    private void EnableDebugShowCollisionDetectionZone(bool toEnable) {
        DrawColliderGizmo drawCntl = gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugCollisionDetectionZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowCollisionDetectionZone(DebugValues.Instance.ShowShipCollisionDetectionZones);
    }

    private void CleanupDebugShowCollisionDetectionZone() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showShipCollisionDetectionZonesChanged -= ShowDebugCollisionDetectionZonesChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion


}

