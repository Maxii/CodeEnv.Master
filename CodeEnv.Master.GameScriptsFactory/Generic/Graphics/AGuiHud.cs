// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiHud.cs
// Abstract Singleton Base class for HUDs drawn by the Gui Camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Abstract Singleton Base class for HUDs drawn by the Gui Camera.
/// </summary>
public abstract class AGuiHud<T> : AMonoBehaviourBaseSingleton<T>, IGuiHud where T : AGuiHud<T> {

    // Camera used to draw this HUD
    public Camera uiCamera;

    protected Transform _transform;
    protected UILabel _label;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        _transform = transform;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        InitializeOnStart();
    }

    protected virtual void InitializeOnStart() {
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
    /// Move the HUD to track an object. Default remains stationary.
    /// </summary>
    protected virtual void UpdatePosition() { }

    public void SetPivot(UIWidget.Pivot pivot) {
        _label.pivot = pivot;
    }

    protected override void OnApplicationQuit() {
        _instance = null;
    }

    #region IGuiHud Members

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

    public void Set(StringBuilder sb) {
        Set(sb.ToString());
    }

    public void Clear() {
        Set(string.Empty);
    }

    #endregion
}

