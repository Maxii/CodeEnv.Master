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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Base class for GUI Buttons built with NGUI.
/// </summary>
public abstract class AGuiButtonBase : GuiTooltip {

    protected GameEventManager _eventMgr;
    protected PlayerPrefsManager _playerPrefsMgr;
    protected UIButton _button;

    protected override void Awake() {
        base.Awake();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _eventMgr = GameEventManager.Instance;
    }

    protected override void Start() {
        base.Start();
        _button = gameObject.GetSafeMonoBehaviourComponent<UIButton>();
    }

    void OnClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    protected abstract void OnLeftClick();

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

