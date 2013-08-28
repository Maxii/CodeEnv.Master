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
using CodeEnv.Master.Common.Unity;
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

    protected override void Awake() {
        base.Awake();
        Target = transform;
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>();
        _systemManager = gameObject.GetSafeMonoBehaviourComponent<SystemManager>();
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
        disableComponentOnInvisible = new Component[1] {
            gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>().collider
        };
        disableGameObjectOnInvisible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().gameObject
        };
        disableComponentOnCameraDistance = gameObject.GetComponentsInChildren<Animation>(); // UNCLEAR except under Billboard?
        disableComponentOnCameraDistance.Union<Component>(gameObject.GetSafeMonoBehaviourComponentsInChildren<Orbit>());

        Renderer[] renderersWithoutVisibilityRelays = gameObject.GetComponentsInChildren<Renderer>()
            .Where<Renderer>(r => r.gameObject.GetComponent<VisibilityChangedRelay>() == null).ToArray<Renderer>();
        disableComponentOnCameraDistance.Union<Component>(renderersWithoutVisibilityRelays);
    }

    protected override void OnIsVisibleChanged() {
        base.OnIsVisibleChanged();
        ChangeHighlighting();
    }

    public void ChangeHighlighting() {
        if (!IsVisible || (!_systemManager.IsSelected && !_orbitalPlane.IsFocus)) {
            Highlight(false);
            return;
        }
        if (_orbitalPlane.IsFocus) {
            if (_systemManager.IsSelected) {
                Highlight(true, Highlights.Both);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        Highlight(true, Highlights.Selected);
    }

    private void Highlight(bool toShow, Highlights highlight = Highlights.None) {
        _systemHighlightRenderer.gameObject.SetActive(toShow);
        if (!toShow) {
            return;
        }
        switch (highlight) {
            case Highlights.Focused:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsFocusedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.IsFocusedColor);
                break;
            case Highlights.Selected:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsSelectedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.IsSelectedColor);
                break;
            case Highlights.Both:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsFocusAndSelectedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, UnityDebugConstants.IsFocusAndSelectedColor);
                break;
            case Highlights.None:
            // should never occur as there should always be a highlight color if this highlight object is showing
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected override int EnableBasedOnDistanceToCamera() {
        int distanceToCamera = Constants.Zero;
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            if (_trackingLabel == null) {
                _trackingLabel = InitializeTrackingLabel();
            }
            distanceToCamera = base.EnableBasedOnDistanceToCamera();
            bool toShowTrackingLabel = false;
            if (IsVisible) {
                if (distanceToCamera == Constants.Zero) {
                    distanceToCamera = Target.DistanceToCameraInt();
                }
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
            }
            //Logger.Log("SystemTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        __SetTrackingLabelShowDistance();
        Star star = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, star.transform.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = GuiTrackingLabelFactory.CreateGuiTrackingLabel(Target, pivotOffset, trackingLabelOffsetFromPivot);
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

