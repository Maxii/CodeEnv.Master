// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Tooltip.cs
// Singleton. My version of UITooltip with naming changes for better understanding.
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
/// Singleton. My version of UITooltip with naming changes for better understanding.
/// </summary>
[Obsolete]
public class Tooltip : AMonoSingleton<TooltipHudWindow> {

    public static bool IsVisible { get { return (_instance != null && _instance._targetVisibility == 1F); } }

    public UILabel label;

    /// <summary>
    /// The speed at which the tooltip will appear or disappear.
    /// Higher values speed up the transitions.
    /// </summary>
    [Tooltip("Higher is faster")]
    public float fadeSpeed = 10F;

    /// <summary>
    /// The target visibility of the tooltip, a value that is either 0 or 1.
    /// Used with _currentVisibility to Lerp from 0 to 1 or 1 to 0.
    /// </summary>
    private float _targetVisibility = 0F;

    /// <summary>
    /// The current visibility of the tooltip, a value between 0 and 1.
    /// Used to track how far tooltip fading in/out has progressed toward its _targetVisibility. 
    /// </summary>
    private float _currentVisibility = 0F;
    private GameObject _hoveredObject;
    private Camera _uiCamera;
    private UIWidget[] _widgets;
    private GameTime _gameTime;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        //TODO  
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameTime = GameTime.Instance;
        _widgets = gameObject.GetSafeMonoBehavioursInChildren<UIWidget>();
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        SetAlpha(Constants.ZeroF);
    }

    #endregion

    /// <summary>
    /// Fade the tooltip in or out to reach the _targetVisibility of 0 or 1.
    /// </summary>
    protected override void Update() {
        base.Update();
        if (_hoveredObject != UICamera.hoveredObject) {
            _hoveredObject = null;
            _targetVisibility = 0F;
        }

        if (_currentVisibility != _targetVisibility) {
            _currentVisibility = Mathf.Lerp(_currentVisibility, _targetVisibility, _gameTime.DeltaTime * fadeSpeed);
            if (Mathf.Abs(_currentVisibility - _targetVisibility) < 0.001f) {
                _currentVisibility = _targetVisibility;
            }
            SetAlpha(_currentVisibility * _currentVisibility);  // fading is slow at the beginning, then accelerates
        }
    }

    private void SetAlpha(float alphaValue) {
        for (int i = 0; i < _widgets.Length; i++) {
            UIWidget widget = _widgets[i];
            Color color = widget.color;
            color.a = alphaValue;
            widget.color = color;
        }
    }

    private void SetText(string tooltipText) {
        if (label != null && !string.IsNullOrEmpty(tooltipText)) {
            _targetVisibility = 1F;
            _hoveredObject = UICamera.hoveredObject;
            label.text = tooltipText;

            // Determine the dimensions of the printed text
            Vector2 textSizeInPixels = label.printedSize;

            // Scale by the transform and adjust by the padding offset
            Vector3 textScale = label.transform.localScale; // probably to accommodate UILabel.Overflow.ShrinkContent
            textSizeInPixels.x *= textScale.x;
            textSizeInPixels.y *= textScale.y;

            // Since the screen can be of different than expected size, we want to convert mouse coordinates to view space, then convert that to world position.

            var mouseScreenSpacePosition = Input.mousePosition; // mouse position in pixel coordinates (0,0) bottomLeft to (screenWidth, screenHeight) topRight

            float mouseViewportSpacePositionX = Mathf.Clamp01(mouseScreenSpacePosition.x / Screen.width);
            float mouseViewportSpacePositionY = Mathf.Clamp01(mouseScreenSpacePosition.y / Screen.height);
            Vector3 mouseViewportSpacePosition = new Vector3(mouseViewportSpacePositionX, mouseViewportSpacePositionY);

            // The ratio of the camera's target orthographic size to current screen size
            float activeSize = _uiCamera.orthographicSize / transform.parent.lossyScale.y;
            float ratio = (Screen.height * 0.5F) / activeSize;
            // The maximum on-screen size of the tooltip window
            Vector2 maxTooltipWindowViewportSpaceSize = new Vector2(ratio * textSizeInPixels.x / Screen.width, ratio * textSizeInPixels.y / Screen.height);

            // Limit the tooltip to always be visible
            float maxTooltipViewportSpacePositionX = 1F - maxTooltipWindowViewportSpaceSize.x;
            float maxTooltipViewportSpacePositionY = maxTooltipWindowViewportSpaceSize.y;
            float tooltipViewportSpacePositionX = Mathf.Min(mouseViewportSpacePosition.x, maxTooltipViewportSpacePositionX);
            float tooltipViewportSpacePositionY = Mathf.Max(mouseViewportSpacePosition.y, maxTooltipViewportSpacePositionY);

            Vector3 tooltipViewportSpacePosition = new Vector3(tooltipViewportSpacePositionX, tooltipViewportSpacePositionY);

            // Position the tooltip in world space
            Vector3 tooltipWorldSpacePosition = _uiCamera.ViewportToWorldPoint(tooltipViewportSpacePosition);
            transform.position = tooltipWorldSpacePosition;
        }
        else {
            _hoveredObject = null;
            _targetVisibility = 0F;
        }
    }

    public static void Show(string text) {
        if (_instance != null) {
            _instance.SetText(text);
        }
    }

    public static void Hide() {
        if (_instance != null) {
            _instance._hoveredObject = null;
            _instance._targetVisibility = 0F;
        }
    }

    #region Cleanup

    protected override void Cleanup() {
        _instance = null;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

