// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHud.cs
// HUD that follows the Cursor on the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// HUD that follows the Cursor on the screen.
/// </summary>
public sealed class GuiCursorHud : AMonoBehaviourBaseSingleton<GuiCursorHud> {

    // Camera used to draw this HUD
    public Camera uiCamera;

    private Transform _transform;
    private UILabel _label;

    void Awake() {
        _transform = transform;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        _label.depth = 100; // draw on top of other Gui Elements in the same Panel
        NGUITools.SetActive(_label.gameObject, false);  //begin deactivated so label doesn't show
        if (uiCamera == null) {
            uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        }
    }

    void Update() {
        if (ToUpdate()) {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Move the HUD to track the cursor.
    /// </summary>
    private void UpdatePosition() {
        if (NGUITools.GetActive(_label.gameObject)) {
            Vector3 cursorPosition = Input.mousePosition;

            if (uiCamera != null) {
                // Since the screen can be of different than expected size, we want to convert
                // mouse coordinates to view space, then convert that to world position.
                cursorPosition.x = Mathf.Clamp01(cursorPosition.x / Screen.width);
                cursorPosition.y = Mathf.Clamp01(cursorPosition.y / Screen.height);
                _transform.position = uiCamera.ViewportToWorldPoint(cursorPosition);
                // OPTIMIZE why not just use uiCamera.ScreenToWorldPoint(cursorPosition)?

                // For pixel-perfect results
                if (uiCamera.isOrthoGraphic) {
                    _transform.localPosition = NGUIMath.ApplyHalfPixelOffset(_transform.localPosition, _transform.localScale);
                }
            }
            else {
                // Simple calculation that assumes that the camera is of fixed size
                cursorPosition.x -= Screen.width * 0.5f;
                cursorPosition.y -= Screen.height * 0.5f;
                _transform.localPosition = NGUIMath.ApplyHalfPixelOffset(cursorPosition, _transform.localScale);
            }
        }
    }

    /// <summary>
    /// Populate the HUD with text.
    /// </summary>
    /// <param name="text">The text to place in the HUD.</param>
    public void Set(string text) {
        if (Instance != null) {
            if (Utility.CheckForContent(text)) {
                if (!NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, true);
                }
                _label.text = text;
                _label.MakePixelPerfect();
                UpdatePosition();
            }
            else {
                if (NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, false);
                }
            }
        }
    }

    /// <summary>
    /// Populate the HUD with text from the StringBuilder.
    /// </summary>
    /// <param name="sb">The StringBuilder containing the text.</param>
    public void Set(StringBuilder sb) {
        Set(sb.ToString());
    }

    /// <summary>
    /// Populate the HUD with text from the GuiCursorHudText.
    /// </summary>
    /// <param name="guiCursorHudText">The GUI cursor hud text.</param>
    public void Set(GuiCursorHudText guiCursorHudText) {
        Set(guiCursorHudText.GetText());
    }

    /// <summary>
    /// Clear the HUD so only the cursor shows.
    /// </summary>
    public void Clear() {
        Set(string.Empty);
    }

    public void SetPivot(UIWidget.Pivot pivot) {
        _label.pivot = pivot;
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

