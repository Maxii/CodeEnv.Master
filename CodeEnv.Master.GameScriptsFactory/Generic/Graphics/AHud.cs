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

// default namespace

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract Singleton Base class for HUDs drawn by the Gui Camera.
/// </summary>
public abstract class AHud<T> : AMonoBaseSingleton<T>, IHud where T : AHud<T> {

    // Camera used to draw this HUD
    public Camera uiCamera;

    protected UILabel _label;
    protected bool _isDisplayEnabled = true;

    protected override void Awake() {
        base.Awake();
        _label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        _label.depth = 100; // draw on top of other Gui Elements in the same Panel
        NGUITools.SetActive(_label.gameObject, false);  //begin deactivated so label doesn't show
        if (uiCamera == null) {
            uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        }
    }

    protected virtual void UpdatePosition() { }

    public void SetPivot(UIWidget.Pivot pivot) {
        _label.pivot = pivot;
    }

    #region IGuiHud Members

    public void Set(string text) {
        if (Instance && _isDisplayEnabled) {
            if (Utility.CheckForContent(text)) {
                if (!NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, true);
                }
                _label.text = text;
                _label.MakePixelPerfect();
                UpdatePosition();
            }
            else {
                _label.text = text;
                if (NGUITools.GetActive(_label.gameObject)) {
                    NGUITools.SetActive(_label.gameObject, false);
                }
            }
        }
    }

    public void Set(StringBuilder sb) {
        Set(sb.ToString());
    }

    public void Clear() {
        Set(string.Empty);
    }

    #endregion
}

