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
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Selection readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiSelectedReadout : AGuiLabelReadoutBase, IDisposable {

    private IList<IDisposable> _subscribers;
    private SelectionManager _selectionMgr;

    protected override void Awake() {
        base.Awake();
        _selectionMgr = SelectionManager.Instance;
        Subscribe();
    }

    protected override void InitializeTooltip() {
        tooltip = "The currently selected game object.";
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_selectionMgr.SubscribeToPropertyChanged<SelectionManager, ISelectable>(sm => sm.CurrentSelection, OnCurrentSelectionChanged));
    }

    private void OnCurrentSelectionChanged() {
        string selectionName = string.Empty;
        ISelectable newSelection = _selectionMgr.CurrentSelection;
        if (newSelection != null) {
            //selectionName = (newSelection as IHasData).GetData().Name;  // IMPROVE ISelectable perviously inherited from IHasData
            selectionName = (newSelection as Component).gameObject.GetSafeMonoBehaviourComponent<AItem>().Data.Name;
        }
        RefreshReadout(selectionName);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
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

