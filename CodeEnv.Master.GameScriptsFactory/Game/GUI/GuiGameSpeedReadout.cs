// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedReadout.cs
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
public class GuiGameSpeedReadout : GuiLabelReadoutBase, IDisposable {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        AddListeners();
        tooltip = "The multiple of Normal Speed the game is currently running at.";
        // don't rely on outside events to initialize
        RefreshGameSpeedReadout(PlayerPrefsManager.Instance.GameSpeedOnLoad);
    }

    private void AddListeners() {
        eventMgr.AddListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
    }

    void OnGameSpeedChange(GameSpeedChangeEvent e) {
        RefreshGameSpeedReadout(e.GameSpeed);
    }

    private void RemoveListeners() {
        eventMgr.RemoveListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
    }

    private void RefreshGameSpeedReadout(GameClockSpeed clockSpeed) {
        readoutLabel.text = CommonTerms.MultiplySign + clockSpeed.SpeedMultiplier().ToString();
    }

    void OnDestroy() {
        Dispose();
    }

    #region IDisposable
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

