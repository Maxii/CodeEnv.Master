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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Base class for GUI Buttons built with NGUI.
/// </summary>
public abstract class AGuiButtonBase : GuiTooltip {

    protected GameEventManager eventMgr;
    protected PlayerPrefsManager playerPrefsMgr;
    protected UIButton button;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        playerPrefsMgr = PlayerPrefsManager.Instance;
        eventMgr = GameEventManager.Instance;
    }

    void Start() {
        InitializeOnStart();
    }

    /// <summary>
    /// Override to initialize the tooltip message. Remember base.InitializeOnAwake();
    /// </summary>
    protected virtual void InitializeOnStart() {
        button = gameObject.GetSafeMonoBehaviourComponent<UIButton>();
        UIEventListener.Get(gameObject).onClick += OnButtonClick;  // NGUI general event system
    }

    protected abstract void OnButtonClick(GameObject sender);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

