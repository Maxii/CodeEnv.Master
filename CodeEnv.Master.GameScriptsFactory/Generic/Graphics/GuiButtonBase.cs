// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiButtonBase.cs
// Base class for GUI Buttons built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Base class for GUI Buttons built with NGUI.
/// </summary>
public abstract class GuiButtonBase : GuiTooltip {

    protected GameEventManager eventMgr;
    protected PlayerPrefsManager playerPrefsMgr;
    protected UIButton button;
    protected bool isInitialized;

    void Awake() {
        playerPrefsMgr = PlayerPrefsManager.Instance;
        eventMgr = GameEventManager.Instance;
    }

    void Start() {
        Initialize();
    }

    /// <summary>
    /// Override to initialize the tooltip message. Remember base.Initialize();
    /// </summary>
    protected virtual void Initialize() {
        button = gameObject.GetSafeMonoBehaviourComponent<UIButton>();
        UIEventListener.Get(gameObject).onClick += OnButtonClick;  // NGUI general event system
        isInitialized = true;
    }

    protected abstract void OnButtonClick(GameObject sender);

    // IDisposable Note: No reason to remove Ngui event listeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

