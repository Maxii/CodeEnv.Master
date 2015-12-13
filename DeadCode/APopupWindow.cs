// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APopupWindow.cs
// Singleton. COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. COMMENT 
/// </summary>
[Obsolete]
public abstract class APopupWindow<T> : AMonoSingleton<T>, IHudWindow where T : APopupWindow<T> {

    public bool IsShowing { get { return _targetAlpha == Constants.OneF; } }

    /// <summary>
    /// The speed at which the tooltip will appear or disappear.
    /// Higher values speed up the transitions.
    /// </summary>
    [Tooltip("Higher is faster")]
    public float fadeSpeed = 10F;

    protected UIWidget _backgroundWidget;

    /// <summary>
    /// The target alpha of the tooltip panel, a value that is either 0 or 1.
    /// Used with _currentAlpha to Lerp from 0 to 1 or 1 to 0.
    /// </summary>
    private float _targetAlpha = 0F;

    /// <summary>
    /// The current alpha of the tooltip panel, a value between 0 and 1.
    /// Used to track how far tooltip fading in/out has progressed toward its _targetAlpha. 
    /// </summary>
    private float _currentAlpha = 0F;
    //private GameObject _hoveredObject;
    private Camera _uiCamera;
    private IDictionary<HudFormID, AHudForm> _tooltipElementLookup;
    private GameTime _gameTime;
    private MyEnvelopContent _backgroundEnvelopContent;
    private UIPanel _panel;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        //References.PopupElement = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AcquireReferences();
        InitializeElementLookup();
        SetAlpha(Constants.ZeroF);
        ActivateElement(null);
    }

    #endregion

    private void AcquireReferences() {
        _gameTime = GameTime.Instance;
        _panel = gameObject.GetSafeMonoBehaviour<UIPanel>();
        _backgroundEnvelopContent = gameObject.GetSafeFirstMonoBehaviourInImmediateChildrenOnly<MyEnvelopContent>();
        //TODO set envelopContent padding programmatically once background is permanently picked?
        _backgroundWidget = _backgroundEnvelopContent.gameObject.GetSafeMonoBehaviour<UIWidget>();
        _uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
    }

    private void InitializeElementLookup() {
        var tooltipElements = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<AHudForm>();
        _tooltipElementLookup = tooltipElements.ToDictionary(e => e.FormID);
    }

    /// <summary>
    /// Fade the tooltip in or out to reach the _targetAlpha of 0 or 1.
    /// </summary>
    protected override void Update() {
        base.Update();
        //if (_hoveredObject != UICamera.hoveredObject) {
        //    _hoveredObject = null;
        //    _targetAlpha = 0F;
        //}

        if (_currentAlpha != _targetAlpha) {
            _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, _gameTime.DeltaTime * fadeSpeed);
            if (Mathf.Abs(_currentAlpha - _targetAlpha) < 0.001f) {
                _currentAlpha = _targetAlpha;
            }
            SetAlpha(_currentAlpha * _currentAlpha);  // fading is slow at the beginning, then accelerates
        }
    }

    private void SetAlpha(float alphaValue) {
        _panel.alpha = alphaValue;
    }

    /// <summary>
    /// Activates the specified tooltipElement's gameObject, deactivating all others.
    /// If tooltipElement is null, all element gameObjects are deactivated.
    /// </summary>
    /// <param name="tooltipElement">The tooltip element.</param>
    private void ActivateElement(AHudForm tooltipElement) {
        _tooltipElementLookup.Values.ForAll(element => {
            if (element == tooltipElement) {
                NGUITools.SetActive(element.gameObject, true);
            }
            else {
                NGUITools.SetActive(element.gameObject, false);
            }
        });
    }

    private void EncompassElementWithBackground(AHudForm element) {
        _backgroundEnvelopContent.targetRoot = element.transform;
        _backgroundEnvelopContent.Execute();
    }

    private void SetContent(AHudFormContent content) {
        _targetAlpha = 1F;
        //_hoveredObject = UICamera.hoveredObject;

        var tooltipElement = _tooltipElementLookup[content.FormID];
        ActivateElement(tooltipElement);
        tooltipElement.FormContent = content;
        EncompassElementWithBackground(tooltipElement);

        PositionPopup();
    }

    protected abstract void PositionPopup(); // { } // {
    //    // Since the screen can be of different than expected size, we want to convert mouse coordinates to view space, then convert that to world position.

    //    var mouseScreenSpacePosition = Input.mousePosition; // mouse position in pixel coordinates (0,0) bottomLeft to (screenWidth, screenHeight) topRight
    //    //D.Log("Mouse.ScreenPositionInPixels = {0}, ScreenWidth = {1}, ScreenHeight = {2}.", mouseScreenSpacePosition, Screen.width, Screen.height);

    //    float mouseViewportSpacePositionX = Mathf.Clamp01(mouseScreenSpacePosition.x / Screen.width);
    //    float mouseViewportSpacePositionY = Mathf.Clamp01(mouseScreenSpacePosition.y / Screen.height);
    //    Vector3 mouseViewportSpacePosition = new Vector3(mouseViewportSpacePositionX, mouseViewportSpacePositionY);

    //    // The maximum on-screen size of the tooltip window
    //    Vector2 maxTooltipWindowViewportSpaceSize = new Vector2((float)_backgroundWidget.width / Screen.width, (float)_backgroundWidget.height / Screen.height);

    //    // Limit the tooltip to always be visible
    //    float maxTooltipViewportSpacePositionX = 1F - maxTooltipWindowViewportSpaceSize.x;
    //    float maxTooltipViewportSpacePositionY = maxTooltipWindowViewportSpaceSize.y;
    //    float tooltipViewportSpacePositionX = Mathf.Min(mouseViewportSpacePosition.x, maxTooltipViewportSpacePositionX);
    //    float tooltipViewportSpacePositionY = Mathf.Max(mouseViewportSpacePosition.y, maxTooltipViewportSpacePositionY);

    //    Vector3 tooltipViewportSpacePosition = new Vector3(tooltipViewportSpacePositionX, tooltipViewportSpacePositionY);
    //    //D.Log("{0}.ViewportSpacePosition = {1}.", GetType().Name, tooltipViewportSpacePosition);

    //    // Position the tooltip in world space
    //    Vector3 tooltipWorldSpacePosition = _uiCamera.ViewportToWorldPoint(tooltipViewportSpacePosition);
    //    transform.position = tooltipWorldSpacePosition;
    //}

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the provided content.
    /// </summary>
    /// <param name="content">The content.</param>
    public void Show(AHudFormContent content) {
        SetContent(content);
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the provided text.
    /// </summary>
    /// <param name="text">The text.</param>
    public void Show(string text) {
        if (!text.IsNullOrEmpty()) {
            Show(new TextHudFormContent(text));
        }
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the content of the StringBuilder.
    /// </summary>
    /// <param name="coloredStringBuilder">The StringBuilder containing text.</param>
    public void Show(StringBuilder stringBuilder) {
        Show(new TextHudFormContent(stringBuilder.ToString()));
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the content of the ColoredStringBuilder.
    /// </summary>
    /// <param name="coloredStringBuilder">The ColoredStringBuilder containing colorized text.</param>
    public void Show(ColoredStringBuilder coloredStringBuilder) {
        Show(new TextHudFormContent(coloredStringBuilder.ToString()));
    }

    /// <summary>
    /// Hide the tooltip.
    /// </summary>
    public void Hide() {
        //_hoveredObject = null;
        _targetAlpha = Constants.ZeroF;
    }

    #region Cleanup

    protected override void Cleanup() {
        _instance = null;
        //References.PopupElement = null;
    }

    #endregion


}

