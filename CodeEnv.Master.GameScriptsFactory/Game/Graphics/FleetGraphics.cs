// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetGraphics.cs
// Handles graphics optimization for Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Handles graphics optimization for Fleets. Assumes location is with Fleet
/// game object, not FleetAdmiral.
/// </summary>
public class FleetGraphics : AGraphics, IDisposable {

    private UISprite _fleetIcon;

    public bool enableTrackingLabel = false;
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.05F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;
    private GuiTrackingLabel _trackingLabel;
    private GuiTrackingLabelFactory _trackingLabelFactory;

    public float circleScaleFactor = .03F;
    private HighlightCircle _circles;
    private VectorLineFactory _vectorLineFactory;
    private VelocityRay _velocityRay;

    // cached references
    private FleetCommand _fleetCmd;
    private FleetManager _fleetMgr;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponent<FleetManager>();
        _fleetCmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
        _trackingLabelFactory = GuiTrackingLabelFactory.Instance;
        _vectorLineFactory = VectorLineFactory.Instance;
        Target = _fleetCmd.transform;
        InitializeHighlighting();
        maxAnimateDistance = 1; // FIXME maxAnimateDistance not used, this is a dummy value to avoid the warning in AGraphics
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnInvisible = new Component[1] { 
            Target.collider 
        };
        disableGameObjectOnInvisible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().gameObject
        };
    }

    private void InitializeHighlighting() {
        _fleetIcon = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
    }

    protected override int EnableBasedOnDistanceToCamera() {
        int distanceToCamera = base.EnableBasedOnDistanceToCamera();
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            bool toShowTrackingLabel = false;
            if (IsVisible) {
                distanceToCamera = distanceToCamera == Constants.Zero ? Target.DistanceToCameraInt() : distanceToCamera;    // not really needed
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
            }
            //D.Log("FleetTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        // use LeadShip collider for the offset rather than the Admiral collider as the Admiral collider changes scale dynamically. 
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, _fleetMgr.LeadShipCaptain.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = _trackingLabelFactory.CreateGuiTrackingLabel(Target, pivotOffset, trackingLabelOffsetFromPivot);
        trackingLabel.IsShowing = true;
        return trackingLabel;
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    public void AssessHighlighting() {
        if (!IsVisible) {
            Highlight(false, Highlights.None);
            return;
        }
        if (_fleetCmd.IsFocus) {
            if (_fleetMgr.IsSelected) {
                Highlight(true, Highlights.SelectedAndFocus);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        if (_fleetMgr.IsSelected) {
            Highlight(true, Highlights.Selected);
            return;
        }
        Highlight(true, Highlights.None);
    }

    private void Highlight(bool toShowFleetIcon, Highlights highlight) {
        if (_fleetIcon != null) {
            _fleetIcon.gameObject.SetActive(toShowFleetIcon);
        }
        ShowVelocityRay(toShowFleetIcon);
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void ShowCircle(bool toShow, Highlights highlight) {
        D.Assert(highlight == Highlights.Focused || highlight == Highlights.Selected);
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = Screen.height * circleScaleFactor;
            _circles = _vectorLineFactory.MakeInstance("FleetCircles", _fleetCmd.transform, normalizedRadius, isRadiusDynamic: false, maxCircles: 2);
            _circles.Colors = new GameColor[2] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor };
            _circles.Widths = new float[2] { 2F, 2F };
        }
        _circles.ShowCircle(toShow, (int)highlight);
    }

    /// <summary>
    /// Shows a Vectrosity Ray indicating the course and speed of the fleet.
    /// </summary>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> speedReference = new Reference<float>(() => _fleetCmd.Data.CurrentSpeed);
                _velocityRay = _vectorLineFactory.MakeInstance("FleetVelocityRay", _fleetCmd.transform, speedReference, GameColor.Green);
            }
            _velocityRay.ShowRay(toShow);
        }
    }

    protected override void OnIsVisibleChanged() {
        base.OnIsVisibleChanged();
        AssessHighlighting();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_velocityRay != null) {
            Destroy(_velocityRay.gameObject);
            _velocityRay = null;
        }
        if (_circles != null) {
            Destroy(_circles.gameObject);
            _circles = null;
        }
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
            _trackingLabel = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

