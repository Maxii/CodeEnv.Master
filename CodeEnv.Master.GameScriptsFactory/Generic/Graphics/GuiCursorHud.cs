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

using System;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// HUD that follows the Cursor on the screen.
/// </summary>
public sealed class GuiCursorHud : AMonoBehaviourBaseSingleton<GuiCursorHud>, IGuiCursorHud {

    // Camera used to draw this HUD
    public Camera uiCamera;

    private Transform _transform;
    //private GameEventManager _eventMgr;
    private UILabel _label;

    void Awake() {
        _transform = transform;
        //_eventMgr = GameEventManager.Instance;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        _label.depth = 100; // draw on top of everything
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
        Vector3 cursorPosition = Input.mousePosition;

        if (uiCamera != null) {
            // Since the screen can be of different than expected size, we want to convert
            // mouse coordinates to view space, then convert that to world position.
            cursorPosition.x = Mathf.Clamp01(cursorPosition.x / Screen.width);
            cursorPosition.y = Mathf.Clamp01(cursorPosition.y / Screen.height);
            _transform.position = uiCamera.ViewportToWorldPoint(cursorPosition);

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

    protected override void OnApplicationQuit() {
        instance = null;
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IGuiCursorHud Members

    public void Clear() {
        Set(string.Empty);
    }

    public void Set(string textInLabel) {
        if (Instance != null) {
            if (Utility.CheckForContent(textInLabel)) {
                if (!NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, true);
                }
                _label.text = textInLabel;
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

    public void Set(StringBuilder sbHudText) {
        Set(sbHudText.ToString());
    }

    #endregion

}

