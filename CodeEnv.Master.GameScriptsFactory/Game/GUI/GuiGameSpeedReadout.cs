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
/// COMMENT 
/// </summary>
public class GuiGameSpeedReadout : GuiLabelReadoutBase, IDisposable {

    private GameEventManager eventMgr;
    private PlayerPrefsManager playerPrefsMgr;

    void Awake() {
        eventMgr = GameEventManager.Instance;
        playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    protected override void Initialize() {
        base.Initialize();
        RefreshGameSpeedReadout(playerPrefsMgr.GameSpeedOnLoadPref);
        eventMgr.AddListener<GameSpeedChangeEvent>(OnGameSpeedChange);
        tooltip = "The multiple of Normal Speed the game is currently running at.";
    }

    void OnGameSpeedChange(GameSpeedChangeEvent e) {
        RefreshGameSpeedReadout(e.GameSpeed);
    }

    private void RefreshGameSpeedReadout(GameClockSpeed clockSpeed) {
        readoutLabel.text = CommonTerms.MultiplySign + clockSpeed.GetSpeedMultiplier().ToString();
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            eventMgr.RemoveListener<GameSpeedChangeEvent>(OnGameSpeedChange);
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

