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
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Retained Focus readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiFocusedReadout : AGuiLabelReadoutBase {

    private Transform _retainedFocus;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeTooltip() {
        tooltip = "The last qualified game object in focus.";
    }

    private void Subscribe() {
        _eventMgr.AddListener<FocusSelectedEvent>(this, OnFocusSelected);
        _eventMgr.AddListener<GameItemDestroyedEvent>(this, OnGameItemDestroyed);
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        Transform focusTransform = e.FocusTransform;
        TryRetainingFocus(focusTransform);
    }

    private void TryRetainingFocus(Transform focusTransform) {
        ICameraFocusable focusable = focusTransform.gameObject.GetInterface<ICameraFocusable>();
        if (focusable.IsRetainedFocusEligible) {
            _retainedFocus = focusTransform;
            IHasData iHasData = focusTransform.gameObject.GetInterface<IHasData>();
            string focusName = "No Data";
            if (iHasData != null) {
                focusName = iHasData.GetData().Name;
            }
            RefreshReadout(focusName);
        }
    }

    private void OnGameItemDestroyed(GameItemDestroyedEvent e) {
        ICameraFocusable focusable = e.Source as ICameraFocusable;
        CheckRetainedFocusDestroyed(focusable);
    }

    private void CheckRetainedFocusDestroyed(ICameraFocusable focusable) {
        if (focusable != null && focusable.IsRetainedFocusEligible) {
            Transform focusableTransform = (focusable as Component).transform;
            if (focusableTransform == _retainedFocus) {
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
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _retainedFocus));
        }
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<FocusSelectedEvent>(this, OnFocusSelected);
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

