// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCheckboxBase.cs
// Base class for Gui Checkboxes built with NGUI.
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
/// Base class for Gui Checkboxes built with NGUI.
/// </summary>
public abstract class GuiCheckboxBase : GuiTooltip, IDisposable {

    protected GameEventManager eventMgr;
    protected PlayerPrefsManager playerPrefsMgr;
    protected UICheckbox checkbox;

    void Awake() {
        playerPrefsMgr = PlayerPrefsManager.Instance;
        eventMgr = GameEventManager.Instance;
    }

    void Start() {
        Initialize();
    }

    /// <summary>
    /// Override to initialize the tooltip message. Remember base.InitializeOnAwake();
    /// </summary>
    protected virtual void Initialize() {
        checkbox = gameObject.GetSafeMonoBehaviour<UICheckbox>();
        checkbox.onStateChange += OnCheckboxStateChange;
    }

    protected abstract void OnCheckboxStateChange(bool state);

    #region IDiposable
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <arg item="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            checkbox.onStateChange -= OnCheckboxStateChange;
        }
        // free unmanaged resources here
        alreadyDisposed = true;
    }

    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

