// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiButtonBase.cs
// Base class for GUI Buttons built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Base class for GUI Buttons built with NGUI.
/// </summary>
public abstract class AGuiButtonBase : GuiTooltip {

    protected GameEventManager _eventMgr;
    protected PlayerPrefsManager _playerPrefsMgr;
    protected UIButton _button;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _eventMgr = GameEventManager.Instance;
    }

    void Start() {
        InitializeOnStart();
    }

    /// <summary>
    /// Override to initialize the tooltip message. Remember base.InitializeOnAwake();
    /// </summary>
    protected virtual void InitializeOnStart() {
        _button = gameObject.GetSafeMonoBehaviourComponent<UIButton>();
        //UIEventListener.Get(gameObject).onClick += OnButtonClick;  // NGUI general event system
    }

    //private void OnButtonClick(GameObject sender) {
    //    if (NguiGameInput.IsLeftMouseButtonClick()) {
    //        OnLeftClick();
    //    }
    //}

    void OnClick() {
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            OnLeftClick();
        }
    }

    protected abstract void OnLeftClick();

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

