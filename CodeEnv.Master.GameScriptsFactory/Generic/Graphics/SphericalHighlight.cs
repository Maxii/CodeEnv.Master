// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SphericalHighlight.cs
// Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target. 
/// </summary>
public class SphericalHighlight : AMonoBase, ISphericalHighlight {

    public bool enableTrackingLabel = false;

    public bool enableEditorAlphaControl = false;

    public bool IsShowing { get { return enabled; } }

    public float alpha = Constants.ZeroF;
    public float Alpha {
        get { return alpha; }
        set {
            if (enableEditorAlphaControl) {
                return;
            }
            value = Mathf.Clamp01(value);
            SetProperty<float>(ref alpha, value, "Alpha", AlphaPropChangedEventHandler);
        }
    }

    private GameColor _color;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", ColorPropChangedEventHandler); }
    }

    private IWidgetTrackable _target;
    private Renderer _renderer;
    private Transform _meshTransform;
    private float _radius;
    private float _baseSphereRadius;
    private ITrackingWidget _trackingLabel;
    private string _radiusLabelText = "Highlight\nRadius: {0:0.#}";

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        InitializeMeshRendererMaterial();
        Show(false);
    }

    private void InitializeValuesAndReferences() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _meshTransform = _renderer.transform;
        _baseSphereRadius = _renderer.bounds.size.x / 2F;
        //D.Log("{0} base sphere radius = {1}.", GetType().Name, _baseSphereRadius);
    }

    private void InitializeMeshRendererMaterial() {
        var material = _renderer.material;
        if (!material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
            material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
        }
    }

    public void SetTarget(IWidgetTrackable target, WidgetPlacement labelPlacement = WidgetPlacement.Below) {
        _target = target;

        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }

        if (_trackingLabel != null) {
            if (_trackingLabel.Target != target) {    // eliminates PropertyChanged not changed warning
                _trackingLabel.Target = target;
            }
            _trackingLabel.Placement = labelPlacement;
        }
        UpdatePosition();
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

    protected sealed override void Update() {
        base.Update();
        UpdatePosition();
    }

    private void UpdatePosition() {
        transform.position = _target.Position;
        //D.Log("{0}: Position = {1}.", GetType().Name, transform.position);
    }

    #region Event and Property Change Handlers

    private void AlphaPropChangedEventHandler() {
        _renderer.material.SetAlpha(Alpha);
    }

    private void ColorPropChangedEventHandler() {
        _renderer.material.color = Color.ToUnityColor();
    }

    #endregion

    protected void ValidateReuseable() {
        D.Assert(_target == null);
        D.Assert(Color == GameColor.None);
        D.Assert(!enableEditorAlphaControl, "{0} spawned with EditorAlphaControl enabled.", gameObject.name);
        D.Assert(Alpha == Constants.ZeroF, "{0}.Alpha {1} should be Zero.", GetType().Name, Alpha);
        //D.Log("{0}.ValidateReuseable() called.", GetType().Name);
    }

    protected void ResetForReuse() {
        //D.Log("{0}.ResetForReuse called.", GetType().Name);
        _target = null;
        _color = GameColor.None;
        alpha = Constants.ZeroF;
    }

    protected override void Cleanup() {
        References.HoverHighlight = null;
        DestroyTrackingLabel();
    }

    protected void DestroyTrackingLabel() {
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

