// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManager.cs
//  Overall GuiManager that handles enabling/disabling Gui elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Overall GuiManager that handles enabling/disabling Gui elements.
/// </summary>
public class GuiManager : MonoBehaviourBaseSingleton<GuiManager>, IDisposable {

    private Stack<IList<UIPanel>> stackedPanelsToRestore = new Stack<IList<UIPanel>>();
    private UIPanel uiRootPanel;
    private GameEventManager eventMgr;

    void Awake() {
        eventMgr = GameEventManager.Instance;
        AddListeners();
    }

    void Start() {
        // Note: Components that are not active are not found with GetComponentInChildren()!
        CheckDebugSettings();
        uiRootPanel = gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings() {
        if (DebugSettings.DisableGui) {
            Camera guiCamera = gameObject.GetSafeMonoBehaviourComponentInChildren<UICamera>().camera;
            guiCamera.enabled = false;
        }
        if (!DebugSettings.EnableFpsReadout) {
            GameObject fpsReadoutParentGo = gameObject.GetSafeMonoBehaviourComponentInChildren<FpsReadout>().transform.parent.gameObject;
            fpsReadoutParentGo.active = false;
        }
    }

    private void AddListeners() {
        eventMgr.AddListener<GuiVisibilityChangeEvent>(this, OnGuiVisibilityChange);
    }

    private void OnGuiVisibilityChange(GuiVisibilityChangeEvent e) {
        //Debug.Log("OnGuiVisibilityChange event received. GuiVisibilityCmd = {0}.", e.GuiVisibilityCmd);
        switch (e.GuiVisibilityCmd) {
            case GuiVisibilityCommand.MakeVisibleUIPanelsInvisible:
                UIPanel[] allActiveUIRootChildPanels = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIPanel>().Except<UIPanel>(uiRootPanel).ToArray<UIPanel>();
                var panelsToDeactivate = (from p in allActiveUIRootChildPanels where !e.Exceptions.Contains<UIPanel>(p) select p);
                //panelsToDeactivate.ForAll<UIPanel>(p => p.gameObject.active = false);
                panelsToDeactivate.ForAll<UIPanel>(p => NGUITools.SetActive(p.gameObject, false));

                stackedPanelsToRestore.Push(panelsToDeactivate.ToList<UIPanel>());
                break;
            case GuiVisibilityCommand.RestoreUIPanelsVisibility:
                if (stackedPanelsToRestore.Count == 0) {
                    Debug.LogError("The stack holding the lists of UIPanels to restore should not be null or empty!");
                }
                //Arguments.ValidateNotNullOrEmpty<IList<UIPanel>>(stackedPanelsToRestore);
                IList<UIPanel> panelsToRestore = stackedPanelsToRestore.Pop();
                Arguments.ValidateNotNullOrEmpty<UIPanel>(panelsToRestore);
                var panelsToReactivate = from p in panelsToRestore where !e.Exceptions.Contains<UIPanel>(p) select p;
                //panelsToReactivate.ForAll<UIPanel>(p => p.gameObject.active = true);
                panelsToReactivate.ForAll<UIPanel>(p => NGUITools.SetActive(p.gameObject, true));

                break;
            case GuiVisibilityCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.GuiVisibilityCmd));
        }
    }

    private void RemoveListeners() {
        eventMgr.RemoveListener<GuiVisibilityChangeEvent>(this, OnGuiVisibilityChange);
    }


    void OnDestroy() {
        Dispose();
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    #region IDisposable
    [NonSerialized]
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            RemoveListeners();
        }
        // free unmanaged resources here
        alreadyDisposed = true;
    }


    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

