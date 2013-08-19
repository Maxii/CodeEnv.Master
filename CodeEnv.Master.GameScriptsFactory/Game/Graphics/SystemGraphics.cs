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
    private static Color __focusedColor = Color.blue;
    private static Color __selectedColor = Color.yellow;

    /// <summary>
    /// The separation between the pivot point on the 3D object that is tracked
    /// and the tracking label as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    private GuiTrackingLabel _trackingLabel;
    private Star _starManager;
    private MeshRenderer _systemHighlightRenderer;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Target = transform;
        _starManager = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        maxAnimateDistance = AnimationSettings.Instance.MaxSystemAnimateDistance;
        _systemHighlightRenderer = __FindSystemHighlight();
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
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
            .Where<Renderer>(r => r.gameObject.GetSafeMonoBehaviourComponent<VisibilityChangedRelay>() == null).ToArray<Renderer>();
        disableComponentOnCameraDistance.Union<Component>(renderersWithoutVisibilityRelays);
    }

    public enum SystemHighlights { None, Focus, Select }
    public void HighlightSystem(bool toShow, SystemHighlights highlight = SystemHighlights.None) {
        switch (highlight) {
            case SystemHighlights.Focus:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, __focusedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, __focusedColor);
                break;
            case SystemHighlights.Select:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, __selectedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, __selectedColor);
                break;
            case SystemHighlights.None:
                D.Assert(!toShow, "{0} can only be used when turning off the highlight.".Inject(highlight.GetName()));
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
        _systemHighlightRenderer.gameObject.SetActive(toShow);
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
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, _starManager.transform.collider.bounds.extents.y, Constants.ZeroF);
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

