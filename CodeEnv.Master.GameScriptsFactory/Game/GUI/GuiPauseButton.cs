// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseButton.cs
// Custom Gui button control for the main User Paused Button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Custom Gui button control for the main User Paused Button.
/// </summary>
public class GuiPauseButton : GuiPauseResumeOnClick, IDisposable {

#pragma warning disable
    [SerializeField]
    private string Warning = "Do not change PauseRequest public variable.";
#pragma warning restore

    private UILabel pauseButtonLabel;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AddListeners();
        tooltip = "Pause or resume the game.";
    }

    private void AddListeners() {
        eventMgr.AddListener<GamePauseStateChangedEvent>(this, OnPauseGame);
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: false));
        pauseButtonLabel = button.GetComponentInChildren<UILabel>();
        UpdateButtonLabel();
        eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: true));
    }

    // real game pause and resumption events, not just gui pause events which may or may not result in a pause or resumption
    private void OnPauseGame(GamePauseStateChangedEvent e) {
        pauseCommand = e.PauseState == GamePauseState.Paused ? PauseRequest.PriorityPause : PauseRequest.PriorityResume;
        UpdateButtonLabel();
    }

    protected override void OnButtonClick(GameObject sender) {
        // toggle the pauseCommand so the base class sends the correct PauseRequest in the GuiPauseRequestEvent
        pauseCommand = (pauseCommand == PauseRequest.PriorityPause) ? PauseRequest.PriorityResume : PauseRequest.PriorityPause;
        base.OnButtonClick(sender);
    }

    private void UpdateButtonLabel() {
        if (pauseButtonLabel != null) {
            // can be null if GamePauseStateChangedEvent arrives during new game start up before Start() is called. It's OK though
            // as the label will be updated based on the recorded pauseCommand once Start() is called
            pauseButtonLabel.text = (pauseCommand == PauseRequest.PriorityPause) ? UIMessages.ResumeButtonLabel : UIMessages.PauseButtonLabel;
        }
    }

    private void RemoveListeners() {
        eventMgr.RemoveListener<GamePauseStateChangedEvent>(this, OnPauseGame);
    }

    void OnDestroy() {
        Dispose();
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


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

