// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiFocusedReadout.cs
// Retained Focus readout class for the Gui, based on Ngui UILabel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Retained Focus readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiFocusedReadout : AGuiLabelReadoutBase {

    private ICameraFocusable _retainedFocus;


    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeTooltip() {
        tooltip = "The last qualified game object in focus.";
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, ICameraFocusable>(cc => cc.CurrentFocus, OnFocusChanged));
        _eventMgr.AddListener<ItemDeathEvent>(this, OnGameItemDestroyed);
    }

    private void OnFocusChanged() {
        ICameraFocusable focus = CameraControl.Instance.CurrentFocus;
        TryRetainingFocus(focus);
    }

    private void TryRetainingFocus(ICameraFocusable focus) {
        if (focus != null && focus.IsRetainedFocusEligible) {
            _retainedFocus = focus;
            AItem itemWithData = (focus as Component).gameObject.GetSafeMonoBehaviourComponent<AItem>();
            string focusName = "No Data";
            if (itemWithData != null) {
                focusName = itemWithData.Data.Name;
            }
            RefreshReadout(focusName);
        }
    }

    private void OnGameItemDestroyed(ItemDeathEvent e) {
        ICameraFocusable focusable = e.Source as ICameraFocusable;
        CheckRetainedFocusDestroyed(focusable);
    }

    private void CheckRetainedFocusDestroyed(ICameraFocusable focusable) {
        if (focusable != null && focusable.IsRetainedFocusEligible) {
            if (focusable == _retainedFocus) {
                RefreshReadout(string.Empty);
                _retainedFocus = null;
            }
        }
    }

    void OnClick() {
        if (GameInputHelper.IsMiddleMouseButton()) {
            OnMiddleClick();
        }
    }

    private void OnMiddleClick() {
        if (_retainedFocus != null) {
            _retainedFocus.IsFocus = true;
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
        _eventMgr.RemoveListener<ItemDeathEvent>(this, OnGameItemDestroyed);
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

