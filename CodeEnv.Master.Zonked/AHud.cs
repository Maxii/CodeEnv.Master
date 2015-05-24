// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHud.cs
// Abstract Singleton Base class for HUDs drawn by the Gui Camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract Singleton Base class for HUDs drawn by the Gui Camera.
/// </summary>
[System.Obsolete]
public abstract class AHud<T> : AMonoSingleton<T>, IHud where T : AHud<T> {

    /// <summary>
    /// Camera used to draw this HUD on the UI layer.
    /// </summary>
    public Camera uiCamera;

    protected UILabel _label;
    protected UISprite _labelBackground;    // can be null
    protected bool _isDisplayEnabled = true;

    private Transform _labelTransform;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        _labelTransform = _label.transform;
        _label.depth = 100; // draw on top of other Gui Elements in the same Panel
        _labelBackground = gameObject.GetComponentInChildren<UISprite>();
        NGUITools.SetActive(_label.gameObject, false);  //begin deactivated so label doesn't show
        if (uiCamera == null) {
            uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        }
    }

    protected void SetLabelPivot(UIWidget.Pivot pivot) {
        _label.pivot = pivot;
    }

    protected void SetLabelOffset(Vector2 offset) {
        _labelTransform.localPosition = offset;
    }

    /// <summary>
    /// Populate the HUD with text.
    /// </summary>
    /// <param name="text">The text to place in the HUD.</param>
    protected void Set(string text) {
        if (_instance && _isDisplayEnabled) {
            if (Utility.CheckForContent(text)) {
                if (!NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, true);
                }
                _label.text = text;
                _label.MakePixelPerfect();
                // UpdateHudPosition();
            }
            else {
                _label.text = text;
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
    protected void Set(StringBuilder sb) {
        Set(sb.ToString());
    }

    #region IHud Members

    public void Clear() {
        Set(string.Empty);
    }

    #endregion

}


