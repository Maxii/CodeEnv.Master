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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Overall GuiManager that handles the visibility of Gui elements.
/// </summary>
public class GuiManager : AMonoBehaviourBaseSingleton<GuiManager>, IDisposable {

    public bool ReadyForSceneChange { get; private set; }

    private Stack<IList<UIPanel>> _stackedPanelsToRestore = new Stack<IList<UIPanel>>();
    private UIPanel[] _panelsToAlwaysRemainActive;
    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
        UIPanel uiRootPanel = gameObject.GetSafeMonoBehaviourComponent<UIRoot>().gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
        UIPanel tooltipPanel = gameObject.GetSafeMonoBehaviourComponentInChildren<UITooltip>().gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
        UIPanel fpsDebugPanel = gameObject.GetSafeMonoBehaviourComponentInChildren<FpsReadout>().transform.parent.gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
        _panelsToAlwaysRemainActive = new UIPanel[3] { uiRootPanel, tooltipPanel, fpsDebugPanel };
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        CheckDebugSettings();
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

    private void Subscribe() {
        _eventMgr.AddListener<GuiVisibilityButton.GuiVisibilityChangeEvent>(Instance, OnGuiVisibilityChange);
        _eventMgr.AddListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.AddListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
    }

    private void OnLoadSavedGame(LoadSavedGameEvent e) {
        RestoreAllPanels();
    }

    private void OnBuildNewGame(BuildNewGameEvent e) {
        RestoreAllPanels();
    }

    private void RestoreAllPanels() {
        foreach (var panelsToRestore in _stackedPanelsToRestore) {
            foreach (var p in panelsToRestore) {
                NGUITools.SetActive(p.gameObject, true);
                D.Log("Reactivating {0}.", p.gameObject.name);
            }
        }
        ReadyForSceneChange = true;
    }

    private void OnGuiVisibilityChange(GuiVisibilityButton.GuiVisibilityChangeEvent e) {
        D.Log("OnGuiVisibilityChange event received. GuiVisibilityCmd = {0}.", e.GuiVisibilityCmd);
        switch (e.GuiVisibilityCmd) {
            case GuiVisibilityCommand.HideVisibleGuiPanels:
                UIPanel[] activeUIRootChildPanelCandidates = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIPanel>().Except<UIPanel>(_panelsToAlwaysRemainActive).ToArray<UIPanel>();
                var panelsToDeactivate = (from p in activeUIRootChildPanelCandidates where !e.Exceptions.Contains<UIPanel>(p) select p);
                //panelsToDeactivate.ForAll<UIPanel>(p => NGUITools.SetActive(p.gameObject, false));
                foreach (var p in panelsToDeactivate) {
                    NGUITools.SetActive(p.gameObject, false);
                    D.Log("Deactivating {0}.", p.gameObject.name);
                }

                _stackedPanelsToRestore.Push(panelsToDeactivate.ToList<UIPanel>());
                break;
            case GuiVisibilityCommand.RestoreInvisibleGuiPanels:
                D.Assert(_stackedPanelsToRestore.Count != Constants.Zero, "The stack holding the lists of UIPanels to restore should not be empty!");
                IList<UIPanel> panelsToRestore = _stackedPanelsToRestore.Pop();
                Arguments.ValidateNotNullOrEmpty<UIPanel>(panelsToRestore);
                var panelsToReactivate = from p in panelsToRestore where !e.Exceptions.Contains<UIPanel>(p) select p;
                //panelsToReactivate.ForAll<UIPanel>(p => NGUITools.SetActive(p.gameObject, true));
                foreach (var p in panelsToReactivate) {
                    NGUITools.SetActive(p.gameObject, true);
                    D.Log("Reactivating {0}.", p.gameObject.name);
                }
                break;
            case GuiVisibilityCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.GuiVisibilityCmd));
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<GuiVisibilityButton.GuiVisibilityChangeEvent>(Instance, OnGuiVisibilityChange);
        _eventMgr.RemoveListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.RemoveListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
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
            Cleanup();
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

