// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiDateReadout.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiDateReadout : GuiLabelReadoutBase, IDisposable {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AddListeners();
        tooltip = "The current date in the game.";
        UpdateRate = UpdateFrequency.Continuous;
    }

    private void AddListeners() {
        eventMgr.AddListener<GamePauseStateChangingEvent>(this, OnPauseStateChanging);
    }


    void Start() {
        //RefreshDateReadout();
    }

    void Update() {
        if (ToUpdate() && !GameManager.IsGamePaused) {
            RefreshDateReadout();
        }
    }

    private void OnPauseStateChanging(GamePauseStateChangingEvent e) {
        switch (e.PauseState) {
            case GamePauseState.Paused:
                // refresh the date in case the game pauses on load
                RefreshDateReadout();
                break;
            case GamePauseState.Resumed:
                // do nothing
                break;
            case GamePauseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.PauseState));
        }
    }

    private void RefreshDateReadout() {
        readoutLabel.text = GameTime.Date.FormattedDate;
    }

    void OnDestroy() {
        Dispose();
    }

    private void RemoveListeners() {
        eventMgr.RemoveListener<GamePauseStateChangingEvent>(this, OnPauseStateChanging);
    }

    #region IDisposable
    [DoNotSerialize]
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

