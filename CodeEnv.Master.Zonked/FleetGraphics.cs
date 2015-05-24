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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Handles graphics optimization for Fleets. Assumes location is with Fleet
/// game object, not FleetAdmiral.
/// </summary>
[System.Obsolete]
public class FleetGraphics : AGraphics, IDisposable {

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Gets or sets a value indicating whether the object this graphics script
    /// is associated with is detectable by the human player. 
    /// eg. a fleet the human player has no intel about is not detectable.
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
    }


    public bool enableTrackingLabel = false;
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.05F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxTrackingLabelShowDistance;
    private GuiTrackingLabel _trackingLabel;
    private TrackingWidgetFactory _trackingLabelFactory;

    public float circleScaleFactor = .03F;
    private HighlightCircle _circles;
    private VelocityRay _velocityRay;
    private UISprite _fleetIcon;

    // cached references
    private FleetCommand _fleetCmd;
    private FleetUnitCreator _fleetMgr;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviour<FleetUnitCreator>();
        _fleetCmd = gameObject.GetSafeMonoBehaviourInChildren<FleetCommand>();
        _trackingLabelFactory = TrackingWidgetFactory.Instance;
        Target = _fleetCmd.transform;
        InitializeHighlighting();
        maxAnimateDistance = 1; // FIXME maxAnimateDistance not used, this is a dummy value to avoid the warning in AGraphics
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnNotDiscernible = new Component[1] { 
            Target.collider 
        };
        disableGameObjectOnNotDiscernible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourInChildren<Billboard>().gameObject
        };
    }

    private void InitializeHighlighting() {
        _fleetIcon = gameObject.GetSafeMonoBehaviourInChildren<UISprite>();
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnIsVisibleChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = base.EnableBasedOnDistanceToCamera(condition);
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            bool toShowTrackingLabel = false;
            if (condition) {
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

    public override void AssessHighlighting() {
        if (!IsDetectable || !InCameraLOS) {
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
            // TODO audio on/off goes here
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
            _circles = new HighlightCircle("FleetCircles", _fleetCmd.transform, normalizedRadius, parent: DynamicObjectsFolder.Folder,
                isRadiusDynamic: false, maxCircles: 2);
            _circles.Colors = new GameColor[2] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor };
            _circles.Widths = new float[2] { 2F, 2F };
        }
        if (toShow) {
            //D.Log("Fleet attempting to show circle {0}.", highlight.GetName());
            if (!_circles.IsShowing) {
                StartCoroutine(_circles.ShowCircles((int)highlight));
            }
            else {
                _circles.AddCircle((int)highlight);
            }
        }
        else if (_circles.IsShowing) {
            _circles.RemoveCircle((int)highlight);
        }
    }

    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> speedReference = new Reference<float>(() => _fleetCmd.Data.CurrentSpeed);
                _velocityRay = new VelocityRay("FleetVelocityRay", _fleetCmd.transform, speedReference, parent: DynamicObjectsFolder.Folder,
                    width: 2F, color: GameColor.Green);
            }
            if (toShow) {
                if (!_velocityRay.IsShowing) {
                    StartCoroutine(_velocityRay.Show());
                }
            }
            else if (_velocityRay.IsShowing) {
                _velocityRay.Hide();
            }
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        if (_circles != null) {
            _circles.Dispose();
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

