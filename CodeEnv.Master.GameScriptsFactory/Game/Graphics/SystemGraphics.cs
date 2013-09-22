// --------------------------------------------------------------------------------------------------------------------
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
public class SystemGraphics : AGraphics {

    public bool enableTrackingLabel = true;

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    private OrbitalPlane _orbitalPlane;
    private SystemManager _systemManager;

    /// <summary>
    /// The separation between the pivot point on the 3D object that is tracked
    /// and the tracking label as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    private GuiTrackingLabel _trackingLabel;
    private MeshRenderer _systemHighlightRenderer;
    private GuiTrackingLabelFactory _trackingLabelFactory;

    protected override void Awake() {
        base.Awake();
        Target = _transform;
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>();
        _systemManager = gameObject.GetSafeMonoBehaviourComponent<SystemManager>();
        _trackingLabelFactory = GuiTrackingLabelFactory.Instance;
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
        disableGameObjectOnInvisible = new GameObject[1] { _systemHighlightRenderer.gameObject };

        Component[] orbitalPlaneCollider = new Component[1] { gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>().collider };
        Renderer[] renderersWithoutVisibilityRelays = gameObject.GetComponentsInChildren<Renderer>()
            .Where<Renderer>(r => r.gameObject.GetComponent<VisibilityChangedRelay>() == null).ToArray<Renderer>();
        if (disableComponentOnInvisible.IsNullOrEmpty()) {
            disableComponentOnInvisible = new Component[0];
        }
        disableComponentOnInvisible = disableComponentOnInvisible.Union<Component>(renderersWithoutVisibilityRelays)
            .Union<Component>(orbitalPlaneCollider).ToArray();
    }

    protected override void OnIsVisibleChanged() {
        base.OnIsVisibleChanged();
        AssessHighlighting();
    }

    public void AssessHighlighting() {
        if (!IsVisible || (!_systemManager.IsSelected && !_orbitalPlane.IsFocus)) {
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
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.FocusedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.SelectedColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
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
        maxTrackingLabelShowDistance = Mathf.RoundToInt(GameManager.Settings.UniverseSize.Radius() * 2);     // TODO so it shows for now
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

