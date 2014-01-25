// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetView.cs
//  A class for managing the elements of a fleet's UI, those that are not already handled by 
//  the UI classes for ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a fleet's UI, those that are not already handled by the UI classes for ships.
/// </summary>
public class FleetView : ACommandView, ICameraFollowable, IHighlightTrackingLabel {

    public new FleetPresenter Presenter {
        get { return base.Presenter as FleetPresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = false;
    private GuiTrackingLabel _trackingLabel;

    private Transform _cmdIconTransform;
    private VelocityRay _velocityRay;

    protected override void InitializePresenter() {
        Presenter = new FleetPresenter(this);
    }

    protected override void InitializeTrackingTarget() {
        TrackingTarget = Presenter.GetHQElementTransform();
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
        InitializeTrackingLabel();
    }

    protected override void OnPlayerIntelContentChanged() {
        base.OnPlayerIntelContentChanged();
        Presenter.NotifyElementsOfIntelChange();
    }

    protected override void RequestContextMenu(bool isDown) {
        Presenter.RequestContextMenu(isDown);
    }

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAllElementsAttacked();
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        Presenter.OnIsSelectedChanged();
    }

    protected override void Update() {
        base.Update();
        KeepViewOverTarget();
    }

    private void KeepViewOverTarget() {
        if (TrackingTarget != null) {
            _transform.position = TrackingTarget.position;
            _transform.rotation = TrackingTarget.rotation;

            // Notes: _fleetIconPivotOffset is a worldspace offset to the top of the leadship collider and doesn't change with scale, position or rotation
            // The approach below will also work if we want a viewport offset that is a constant percentage of the viewport
            //Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShipTransform.position + _fleetIconPivotOffset);
            //Vector3 worldOffsetLocation = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconViewportOffset);
            //_fleetIconTransform.localPosition = worldOffsetLocation - _leadShipTransform.position;
            _cmdIconTransform.localPosition = _cmdIconPivotOffset;
        }
    }

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            string fleetName = Presenter.Item.PieceName;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(TrackingTarget, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance, Mathf.Infinity, fleetName);
        }
    }

    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> fleetSpeed = Presenter.GetFleetSpeed();
                _velocityRay = new VelocityRay("FleetVelocityRay", _transform, fleetSpeed, parent: DynamicObjects.Folder,
                    width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }

    protected override void InitializeCmdIcon() {
        base.InitializeCmdIcon();
        _cmdIconTransform = _cmdIconSprite.transform;
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

