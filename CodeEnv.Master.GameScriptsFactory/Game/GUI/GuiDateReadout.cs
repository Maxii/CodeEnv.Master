// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiDateReadout.cs
// Date readout class for the Gui, based on Ngui UILabel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Date readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiDateReadout : AGuiLabelReadoutBase, IDisposable {

    private IList<IDisposable> _subscribers;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void InitializeTooltip() {
        tooltip = "The current date in the game.";
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, bool>(gm => gm.IsPaused, OnIsPausedChanging));
    }

    void Update() {
        if (ToUpdate() && !_gameMgr.IsPaused) {
            RefreshReadout(GameTime.Date.FormattedDate);
        }
    }

    private void OnIsPausedChanging(bool isPausing) {
        if (isPausing) {
            // we are about to pause so refresh the date in case the game pauses on load
            RefreshReadout(GameTime.Date.FormattedDate);
        }
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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

