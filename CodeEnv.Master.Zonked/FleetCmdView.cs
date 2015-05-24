﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdView.cs
//  A class for managing the elements of a fleet's UI, those that are not already handled by 
//  the UI classes for ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a fleet's UI, those that are not already handled by the UI classes for ships.
/// </summary>
public class FleetCmdView : AUnitCommandView, IFleetCmdViewable, ICameraFollowable {

    public new FleetCmdPresenter Presenter {
        get { return base.Presenter as FleetCmdPresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = false;
    private ITrackingWidget _trackingLabel;

    private VelocityRay _velocityRay;
    private PathfindingLine _pathfindingLine;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new FleetCmdPresenter(this);
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
        ShowVelocityRay(IsDiscernible);
    }

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }
    }

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        Presenter.OnPlayerIntelCoverageChanged();
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        Presenter.OnIsSelectedChanged();
    }

    #region MouseEvents

    protected override void OnLeftClick() {
        base.OnLeftClick();
        //__ToggleIntelChangingTest();
    }

    #endregion

    #region Intel Change Testing

    private Job __intelTestJob;
    private void __ToggleIntelChangingTest() {
        if (__intelTestJob == null) {
            __intelTestJob = new Job(__CycleIntel());
        }
        if (!__intelTestJob.IsRunning) {
            __intelTestJob.Start();
        }
        else {
            __intelTestJob.Kill();
        }
    }

    private IEnumerator __CycleIntel() {
        while (true) {
            PlayerIntel.CurrentCoverage = Enums<IntelCoverage>.GetRandom(true);
            yield return new WaitForSeconds(4F);
        }
    }

    #endregion

    protected override void Update() {
        base.Update();
        if (TrackingTarget != null) {
            PositionCmdOverTrackingTarget();
        }
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        string fleetName = Presenter.Model.UnitName;
        var trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(TrackingTarget, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.OptionalRootName = fleetName + CommonTerms.Label;
        trackingLabel.Set(fleetName);
        return trackingLabel;
    }

    /// <summary>
    /// Shows a Ray eminating from the Fleet's CommandTransform (tracking the HQ ship) indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (_velocityRay == null) {
                if (!toShow) { return; }
                Reference<float> fleetSpeed = Presenter.GetFleetSpeedReference();
                _velocityRay = new VelocityRay("FleetVelocityRay", _transform, fleetSpeed, width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }

    /// <summary>
    /// Shows the plotted path of the fleet.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="course">The course.</param>
    private void ShowPlottedPath(bool toShow, Vector3[] course) {
        if (course.Any()) {
            if (_pathfindingLine == null) {
                _pathfindingLine = new PathfindingLine("FleetPath", course, Presenter.GetDestinationReference());
            }
            else {
                _pathfindingLine.Points = course;
                _pathfindingLine.Destination = Presenter.GetDestinationReference();
            }
        }

        if (_pathfindingLine != null) {
            _pathfindingLine.Show(toShow);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel, Destroy);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IFleetCmdViewable Members

    public void AssessShowPlottedPath(IList<Vector3> course) {
        bool toShow = course.Count > Constants.Zero && IsSelected;  // OPTIMIZE include IsDiscernible criteria
        ShowPlottedPath(toShow, course.ToArray());
    }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float FollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float FollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

