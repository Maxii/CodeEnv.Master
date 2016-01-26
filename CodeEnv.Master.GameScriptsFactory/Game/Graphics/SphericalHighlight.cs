﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemHighlight.cs
// Singleton spherical highlight control and label that tracks the designated IWidgetTrackable target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton spherical highlight control and label that tracks the designated IWidgetTrackable target.
/// </summary>
public class SphericalHighlight : AMonoSingleton<SphericalHighlight>, ISphericalHighlight {

    [SerializeField]
    private bool _enableTrackingLabel = false;

    [Range(0.1F, 1.0F)]
    [SerializeField]
    private float _alphaValueWhenShowing = 0.20F;

    private IHighlightable _target;
    private Renderer _renderer;
    private Transform _meshTransform;
    private float _radius;
    private float _baseSphereRadius;
    private ITrackingWidget _trackingLabel;
    private string _radiusLabelText = "Highlight\nRadius: {0:0.#}";

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SphericalHighlight = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        InitializeMeshRendererMaterial();
        Show(false);
    }

    private void InitializeValuesAndReferences() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _meshTransform = _renderer.transform;
        _baseSphereRadius = _renderer.bounds.size.x / 2F;
        //D.Log("{0} base sphere radius = {1}.", GetType().Name, _baseSphereRadius);
        UpdateRate = FrameUpdateFrequency.Continuous;   // .Normal is not often enough when moving fast
    }

    private void InitializeMeshRendererMaterial() {
        var material = _renderer.material;
        if (!material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
            material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
        }
        material.SetAlpha(_alphaValueWhenShowing);
    }

    public void SetTarget(IHighlightable target, WidgetPlacement labelPlacement = WidgetPlacement.Below) {
        _target = target;
        if (_enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }

        if (_trackingLabel != null) {
            if (_trackingLabel.Target != target) {    // eliminates PropertyChanged not changed warning
                _trackingLabel.Target = target;
            }
            _trackingLabel.Placement = labelPlacement;
        }
        UpdatePosition();
        SetRadius(target.HoverHighlightRadius);
    }

    private ITrackingWidget InitializeTrackingLabel() {
        var trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(_target);
        trackingLabel.OptionalRootName = GetType().Name;
        return trackingLabel;
    }

    public void SetRadius(float sphereRadius) {
        if (Mathf.Approximately(_radius, sphereRadius)) {
            return;
        }
        _radius = sphereRadius;
        float scaleFactor = sphereRadius / _baseSphereRadius;
        _meshTransform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        //D.Log("{0} Radius = {1}.", GetType().Name, _radiusLabel.text);
        if (_trackingLabel != null) {
            _trackingLabel.Set(_radiusLabelText.Inject(sphereRadius));
            _trackingLabel.SetShowDistance(sphereRadius * 2F);
            // highlight sphere radius has changed so label placement offset has changed
            _trackingLabel.RefreshWidgetValues();
        }
    }

    public void Show(bool toShow) {
        //D.Log("{0}: Show({1}) called.", GetType().Name, toShow);
        _renderer.enabled = toShow; // no CameraLosChangedListener to get in the way!
        enabled = toShow;
        if (_trackingLabel != null) {
            _trackingLabel.Show(toShow);
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        UpdatePosition();
    }

    private void UpdatePosition() {
        transform.position = _target.Position;
        //D.Log("{0}: Position = {1}.", GetType().Name, transform.position);
    }

    protected override void Cleanup() {
        References.SphericalHighlight = null;
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

