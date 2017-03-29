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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target. 
/// </summary>
public class SphericalHighlight : AMonoBase, ISphericalHighlight {

    [Tooltip("Shows a label that tracks the highlight")]
    [SerializeField]
    private bool _enableTrackingLabel = false;

    [Tooltip("Enables manual control of the alpha value from the editor, ignoring programmatic changes.")]
    [SerializeField]
    private bool _enableEditorAlphaControl = false;

    public bool IsShowing { get { return enabled; } }

    [Tooltip("Adjust to change transparency of highlight during Edit")]
    [Range(0F, 1.0F)]
    [SerializeField]
    private float _alpha = Constants.ZeroF;
    /// <summary>
    /// The transparency level of the material. Set this AFTER setting Color and
    /// make sure _enableEditorAlphaControl is not enabled as this change will be
    /// ignored if manually setting from the editor.
    /// </summary>
    public float Alpha {
        get { return _alpha; }
        set {
            if (_enableEditorAlphaControl) {
                D.Warn("{0}: Attempt to set Alpha to {1:0.00} is being ignored with manual control enabled.", GetType().Name, value);
                return;
            }
            value = Mathf.Clamp01(value);
            SetProperty<float>(ref _alpha, value, "Alpha", AlphaPropChangedEventHandler);
        }
    }

    private GameColor _color;
    public GameColor Color {
        get { return _color; }
        set { SetProperty<GameColor>(ref _color, value, "Color", ColorPropChangedEventHandler); }
    }

    public string TargetName { get { return _target != null ? _target.DebugName : "None"; } }

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

    void Update() {
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
        _renderer.material.SetAlpha(Alpha);
    }

    #endregion

    protected void ValidateReuseable() {
        D.AssertNull(_target, gameObject.name);
        D.AssertDefault((int)Color, gameObject.name);
        D.Assert(!_enableEditorAlphaControl, gameObject.name);
        D.AssertEqual(Constants.ZeroF, Alpha, Alpha.ToString());
        //D.Log("{0}.ValidateReuseable() called.", GetType().Name);
    }

    protected void ResetForReuse() {
        //D.Log("{0}.ResetForReuse called.", GetType().Name);
        _target = null;
        _color = GameColor.None;
        _alpha = Constants.ZeroF;
    }

    protected override void Cleanup() {
        GameReferences.HoverHighlight = null;
        DestroyTrackingLabel();
    }

    protected void DestroyTrackingLabel() {
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

