// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSelectedReadout.cs
// Selection readout class for the Gui, based on Ngui UILabel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Selection readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiSelectedReadout : AGuiLabelReadoutBase, IDisposable {

    protected override void Awake() {
        base.Awake();
        tooltip = "The currently selected game object.";
    }

    protected override void Start() {
        base.Start();
        Subscribe();
    }

    private void Subscribe() {
        _eventMgr.AddListener<SelectionEvent>(this, OnNewSelection);
        _eventMgr.AddListener<GameItemDestroyedEvent>(this, OnGameItemDestroyed);
    }

    private void OnNewSelection(SelectionEvent e) {
        ISelectable newSelection = e.Source as ISelectable;
        RefreshReadout(newSelection.GetData().Name);
    }

    private void OnGameItemDestroyed(GameItemDestroyedEvent e) {
        ISelectable selectable = e.Source as ISelectable;
        if (selectable != null) {
            if (selectable.IsSelected) {
                RefreshReadout(string.Empty);
            }
        }
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<SelectionEvent>(this, OnNewSelection);
        _eventMgr.RemoveListener<GameItemDestroyedEvent>(this, OnGameItemDestroyed);
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Unsubscribe();
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

