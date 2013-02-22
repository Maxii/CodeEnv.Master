// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPopupListBase.cs
// Base class for  Gui PopupLists that use Enums built with NGUI.
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
/// Base class for  Gui PopupLists that use Enums built with NGUI. 
/// </summary>
[Obsolete]
public abstract class GuiPopupListBase : GuiTooltip, IDisposable {

    protected GameEventManager eventMgr;
    protected PlayerPrefsManager playerPrefsMgr;
    protected UIPopupList popupList;

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
        popupList = gameObject.GetSafeMonoBehaviourComponent<UIPopupList>();
        popupList.onSelectionChange += OnPopupListSelectionChange;
    }

    protected abstract void OnPopupListSelectionChange(string item);

    protected void WarnOnIncorrectName(string name) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string callerIdMessage = ". Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
        Debug.LogWarning("Name used in PopupList not found: " + name + callerIdMessage);
    }

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
    /// <param item="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            popupList.onSelectionChange -= OnPopupListSelectionChange;
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

