﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemGraphics.cs
// Handles graphics optimization for Systems. Assumes location is with System
// game object.
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
/// Handles graphics optimization for Systems. Assumes location is with System
/// game object.
/// </summary>
[System.Obsolete]
public class SystemGraphics : AGraphics, IDisposable {

    public bool enableTrackingLabel = true;

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    private OrbitalPlaneInputEventRouter _orbitalPlane;
    private SystemCreator _systemManager;

    /// <summary>
    /// The separation between the pivot point on the 3D object that is tracked
    /// and the tracking label as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    private GuiTrackingLabel _trackingLabel;
    private MeshRenderer _systemHighlightRenderer;
    private TrackingWidgetFactory _trackingLabelFactory;

    protected override void Awake() {
        base.Awake();
        Target = _transform;
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlaneInputEventRouter>();
        _systemManager = gameObject.GetSafeMonoBehaviourComponent<SystemCreator>();
        _trackingLabelFactory = TrackingWidgetFactory.Instance;
        maxAnimateDistance = AnimationSettings.Instance.MaxSystemAnimateDistance;
        _systemHighlightRenderer = __FindSystemHighlight();
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    protected override void RegisterComponentsToDisable() {
        disableGameObjectOnNotDiscernible = new GameObject[1] { _systemHighlightRenderer.gameObject };

        Component[] orbitalPlaneCollider = new Component[1] { gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlaneInputEventRouter>().collider };
        Renderer[] renderersWithoutVisibilityRelays = gameObject.GetComponentsInChildren<Renderer>()
            .Where<Renderer>(r => r.gameObject.GetComponent<CameraLOSChangedRelay>() == null).ToArray<Renderer>();
        if (disableComponentOnNotDiscernible.IsNullOrEmpty()) {
            disableComponentOnNotDiscernible = new Component[0];
        }
        disableComponentOnNotDiscernible = disableComponentOnNotDiscernible.Union<Component>(renderersWithoutVisibilityRelays)
            .Union<Component>(orbitalPlaneCollider).ToArray();
    }

    public override void AssessHighlighting() {
        if (!InCameraLOS || (!_systemManager.IsSelected && !_orbitalPlane.IsFocus)) {
            Highlight(false, Highlights.None);
            return;
        }
        if (_orbitalPlane.IsFocus) {
            if (_systemManager.IsSelected) {
                Highlight(true, Highlights.SelectedAndFocus);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        Highlight(true, Highlights.Selected);
    }

    private void Highlight(bool toShow, Highlights highlight) {
        _systemHighlightRenderer.gameObject.SetActive(toShow);
        switch (highlight) {
            case Highlights.Focused:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                // nothing to do as the highlighter should already be inactive
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
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
            //D.Log("SystemTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        __SetTrackingLabelShowDistance();
        Star star = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, star.transform.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = _trackingLabelFactory.CreateGuiTrackingLabel(Target, pivotOffset, trackingLabelOffsetFromPivot);
        trackingLabel.IsShowing = true;
        return trackingLabel;
    }

    private void __SetTrackingLabelShowDistance() {
        maxTrackingLabelShowDistance = Mathf.RoundToInt(GameManager.GameSettings.UniverseSize.Radius() * 2);     // TODO so it shows for now
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        // other cleanup here including any tracking Gui2D elements
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
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

