// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManager.cs
//  Overall GuiManager that handles enabling/disabling Gui elements based on Debug settings.
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
/// Overall GuiManager that handles enabling/disabling Gui elements based on Debug settings.
/// </summary>
public class GuiManager : MonoBehaviourBaseSingleton<GuiManager>, IDisposable {

    private Stack<IList<UIPanel>> stackedPanelsToRestore = new Stack<IList<UIPanel>>();
    private GameEventManager eventMgr;

    void Awake() {
        eventMgr = GameEventManager.Instance;
    }

    void Start() {
        // Note: Components that are not active are not found with GetComponentInChildren()!
        if (DebugSettings.DisableGui) {
            Camera guiCamera = gameObject.GetSafeMonoBehaviourComponentInChildren<UICamera>().camera;
            guiCamera.enabled = false;
        }
        if (!DebugSettings.EnableFpsReadout) {
            GameObject fpsReadoutParentGo = gameObject.GetSafeMonoBehaviourComponentInChildren<FpsReadout>().transform.parent.gameObject;
            fpsReadoutParentGo.active = false;
        }
        eventMgr.AddListener<GuiVisibilityChangeEvent>(OnGuiVisibilityChange);
    }

    private void OnGuiVisibilityChange(GuiVisibilityChangeEvent e) {
        //Debug.Log("OnGuiVisibilityChange event received. GuiVisibilityCmd = {0}.".Inject(e.GuiVisibilityCmd));
        switch (e.GuiVisibilityCmd) {
            case GuiVisibilityCommand.MakeVisibleUIPanelsInvisible:
                UIPanel[] allActiveGuiPanels = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIPanel>();
                var panelsToDeactivate = (from p in allActiveGuiPanels where !e.Exceptions.Contains<UIPanel>(p) select p);
                panelsToDeactivate.ForAll<UIPanel>(p => p.gameObject.active = false);
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
                panelsToReactivate.ForAll<UIPanel>(p => p.gameObject.active = true);
                break;
            case GuiVisibilityCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.GuiVisibilityCmd));
        }
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
            eventMgr.RemoveListener<GuiVisibilityChangeEvent>(OnGuiVisibilityChange);
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

