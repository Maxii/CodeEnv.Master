// --------------------------------------------------------------------------------------------------------------------
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
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a fleet's UI, those that are not already handled by the UI classes for ships.
/// </summary>
public class FleetCmdView : AUnitCommandView, ICameraFollowable, IHighlightTrackingLabel {

    public new FleetCmdPresenter Presenter {
        get { return base.Presenter as FleetCmdPresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = false;
    private GuiTrackingLabel _trackingLabel;

    private VelocityRay _velocityRay;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new FleetCmdPresenter(this);
    }

    protected override void InitializeTrackingTarget() {
        TrackingTarget = Presenter.GetTrackingTarget();
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.gameObject.SetActive(IsDiscernible);
        }
        ShowVelocityRay(IsDiscernible);
    }

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        //must position FleetCmd immediately over tracking target as formationStations are children of FleetCmd
        // if we wait for update, then some ships won't show up as onStation during initialization
        KeepViewOverTarget();
        //D.Log("{0}.TrackingTarget is now {1}. View is positioned over it.", Presenter.Model.FullName, TrackingTarget.name);
        InitializeTrackingLabel();
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
        __ToggleIntelChangingTest();
    }

    #endregion

    #region Intel Change Testing

    private Job _intelTestJob;
    private void __ToggleIntelChangingTest() {
        if (_intelTestJob == null) {
            _intelTestJob = new Job(__CycleIntel());
        }
        if (!_intelTestJob.IsRunning) {
            _intelTestJob.Start();
        }
        else {
            _intelTestJob.Kill();
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
        KeepViewOverTarget();
    }

    private void KeepViewOverTarget() {
        if (TrackingTarget != null) {
            _transform.position = TrackingTarget.position;
            _transform.rotation = TrackingTarget.rotation;
            PositionIcon();
        }
    }

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            string fleetName = Presenter.Model.UnitName;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(TrackingTarget, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance, Mathf.Infinity, fleetName);
        }
    }

    /// <summary>
    /// Shows a Ray eminating from the Fleet's CommandTransform (tracking the HQ ship) indicating its course and speed.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> fleetSpeed = Presenter.GetFleetSpeedReference();
                _velocityRay = new VelocityRay("FleetVelocityRay", _transform, fleetSpeed, parent: _dynamicObjects.Folder,
                    width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
            _trackingLabel = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

    #region IHighlightTrackingLabel Members

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    #endregion

}

