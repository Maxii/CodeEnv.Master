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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
        _subscribers = new List<IDisposable>();
        _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanging<CameraControl, ICameraFocusable>(cc => cc.CurrentFocus, OnFocusChanging));
        _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, ICameraFocusable>(cc => cc.CurrentFocus, OnFocusChanged));
    }

    private void OnFocusChanging(ICameraFocusable newFocus) {
        var previousFocus = CameraControl.Instance.CurrentFocus;
        if (previousFocus != null && previousFocus.IsRetainedFocusEligible) {
            var previousMortalFocus = previousFocus as IMortalItem;
            if (previousMortalFocus != null) {
                previousMortalFocus.onDeathOneShot -= OnRetainedFocusDeath;
            }
        }
    }

    private void OnFocusChanged() {
        ICameraFocusable focus = CameraControl.Instance.CurrentFocus;
        TryRetainingFocus(focus);
    }

    private void TryRetainingFocus(ICameraFocusable focus) {
        if (focus != null && focus.IsRetainedFocusEligible) {
            _retainedFocus = focus;
            var mortalFocus = focus as IMortalItem;
            if (mortalFocus != null) {
                mortalFocus.onDeathOneShot += OnRetainedFocusDeath;
            }
            RefreshReadout(focus.Transform.name);
        }
    }

    private void OnRetainedFocusDeath(IMortalItem mortalItem) {
        D.Assert(mortalItem as ICameraFocusable == _retainedFocus);
        RefreshReadout(string.Empty);
        _retainedFocus = null;
    }

    void OnClick() {
        if (GameInputHelper.Instance.IsMiddleMouseButton()) {
            OnMiddleClick();
        }
    }

    private void OnMiddleClick() {
        if (_retainedFocus != null) {
            _retainedFocus.IsFocus = true;
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
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
        if (_retainedFocus != null) {
            var mortalRetainedFocus = _retainedFocus as IMortalItem;
            if (mortalRetainedFocus != null) {
                mortalRetainedFocus.onDeathOneShot -= OnRetainedFocusDeath;
            }
        }
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

