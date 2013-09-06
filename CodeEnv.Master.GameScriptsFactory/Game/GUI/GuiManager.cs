// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManager.cs
// Overall GuiManager that handles the visibility of Gui elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Overall GuiManager that handles the visibility of Gui elements.
/// </summary>
public class GuiManager : AMonoBehaviourBaseSingleton<GuiManager>, IDisposable {

    private Stack<IList<UIPanel>> _stackedPanelsToRestore = new Stack<IList<UIPanel>>();
    private UIPanel _uiRootPanel;
    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
        AddListeners();
    }

    protected override void Start() {
        base.Start();
        CheckDebugSettings();
        _uiRootPanel = gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings() {
        DebugSettings debugSettings = DebugSettings.Instance;
        if (debugSettings.DisableGui) {
            Camera guiCamera = gameObject.GetSafeMonoBehaviourComponentInChildren<UICamera>().camera;
            guiCamera.enabled = false;
        }
        if (!debugSettings.EnableFpsReadout) {
            GameObject fpsReadoutParentGo = gameObject.GetSafeMonoBehaviourComponentInChildren<FpsReadout>().transform.parent.gameObject;
            fpsReadoutParentGo.SetActive(false);
        }
    }

    private void AddListeners() {
        _eventMgr.AddListener<GuiVisibilityButton.GuiVisibilityChangeEvent>(this, OnGuiVisibilityChange);
    }

    private void OnGuiVisibilityChange(GuiVisibilityButton.GuiVisibilityChangeEvent e) {
        //Logger.Log("OnGuiVisibilityChange event received. GuiVisibilityCmd = {0}.", e.GuiVisibilityCmd);
        switch (e.GuiVisibilityCmd) {
            case GuiVisibilityCommand.HideVisibleGuiPanels:
                UIPanel[] allActiveUIRootChildPanels = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIPanel>().Except<UIPanel>(_uiRootPanel).ToArray<UIPanel>();
                var panelsToDeactivate = (from p in allActiveUIRootChildPanels where !e.Exceptions.Contains<UIPanel>(p) select p);
                //panelsToDeactivate.ForAll<UIPanel>(p => p.gameObject.active = false);
                panelsToDeactivate.ForAll<UIPanel>(p => NGUITools.SetActive(p.gameObject, false));

                _stackedPanelsToRestore.Push(panelsToDeactivate.ToList<UIPanel>());
                break;
            case GuiVisibilityCommand.RestoreInvisibleGuiPanels:
                if (_stackedPanelsToRestore.Count == 0) {
                    D.Error("The stack holding the lists of UIPanels to restore should not be null or empty!");
                }
                //Arguments.ValidateNotNullOrEmpty<IList<UIPanel>>(stackedPanelsToRestore);
                IList<UIPanel> panelsToRestore = _stackedPanelsToRestore.Pop();
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
        _eventMgr.RemoveListener<GuiVisibilityButton.GuiVisibilityChangeEvent>(this, OnGuiVisibilityChange);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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
    /// <arg name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
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

}

