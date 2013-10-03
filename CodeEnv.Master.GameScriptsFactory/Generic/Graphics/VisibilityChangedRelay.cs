// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VisibilityChangedRelay.cs
// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
// that implement the IOnVisible interface.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System;

/// <summary>
/// Simple script that relays changes in its Renderer's visibility state to one or more Target gameobjects
/// that implement the IOnVisible interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
public class VisibilityChangedRelay : AMonoBehaviourBase, IDisposable {

    public Transform[] relayTargets;

    private INotifyVisibilityChanged[] _iRelayTargets;
    private bool _isGameRunning;
    private IList<IDisposable> _subscribers;
    private Renderer _renderer;

    protected override void Awake() {
        base.Awake();
        _renderer = UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        _renderer.enabled = true;   // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!
        if (relayTargets.Length == 0) {
            Transform relayTarget = _transform.GetSafeTransformWithInterfaceInParents<INotifyVisibilityChanged>();
            if (relayTarget != null) {
                D.Warn("{0} {1} target field is not assigned. Automatically assigning {1} as target.", _transform.name, this.GetType().Name, relayTarget);
            }
            else {
                D.Warn("No {0} assigned or found for {1}.", typeof(INotifyVisibilityChanged), _transform.name);
                return;
            }
            relayTargets = new Transform[1] { relayTarget };
        }

        int length = relayTargets.Length;
        _iRelayTargets = new INotifyVisibilityChanged[length];

        for (int i = 0; i < length; i++) {
            INotifyVisibilityChanged iTarget = relayTargets[i].GetInterface<INotifyVisibilityChanged>();
            if (iTarget == null) {
                D.Warn("{0} is not an {1}.", relayTargets[i].name, typeof(INotifyVisibilityChanged));
                continue;
            }
            _iRelayTargets[i] = iTarget;
        }
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsGameRunning, OnIsRunningChanged));
    }

    private void OnIsRunningChanged() {
        _isGameRunning = GameManager.Instance.IsGameRunning;
        if (_isGameRunning) {
            // all relay targets start out initialized with IsVisible = true. This just initializes their list of child meshes that think they are visible
            OnBecameVisible();
        }
    }

    private bool _isVisible;    // starts false so startup OnBecameVisible events don't generate warnings in ValidateVisibiltyChange
    void OnBecameVisible() {
        //D.Log("{0} VisibilityRelay has received OnBecameVisible().", _transform.name);
        if (ValidateVisibilityChange(isVisible: true)) {
            for (int i = 0; i < relayTargets.Length; i++) {
                INotifyVisibilityChanged iNotify = _iRelayTargets[i];
                if (iNotify != null) {
                    LogVisibilityChange(_transform.name, relayTargets[i].name, isVisible: true);
                    iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
                }
            }
            // more efficient and easier but can't provide the client target name for debug
            //foreach (var iNotify in _iRelayTargets) {    
            //    if (iNotify != null) {
            //        iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
            //        D.Log("{0} has notified a client of becoming Visible.", _transform.name);
            //    }
            //}
            _isVisible = true;
        }
    }

    void OnBecameInvisible() {
        //D.Log("{0} VisibilityRelay has received OnBecameInvisible().", _transform.name);
        if (ValidateVisibilityChange(isVisible: false)) {
            for (int i = 0; i < relayTargets.Length; i++) {
                Transform t = relayTargets[i];
                if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                    INotifyVisibilityChanged iNotify = _iRelayTargets[i];
                    if (iNotify != null) {
                        LogVisibilityChange(_transform.name, relayTargets[i].name, isVisible: false);
                        iNotify.NotifyVisibilityChanged(_transform, isVisible: false);
                    }
                }
            }
            _isVisible = false;
        }
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    private void LogVisibilityChange(string notifier, string client, bool isVisible) {
        if (DebugSettings.Instance.EnableVerboseDebugLog) {
            string iNotifyParentName = _transform.GetSafeTransformWithInterfaceInParents<INotifyVisibilityChanged>().name;
            string visibility = isVisible ? "Visible" : "Invisible";
            D.Log("{0} of parent {1} is notifying client {2} of becoming {3}.", notifier, iNotifyParentName, client, visibility);
        }
    }
    // FIXME Recieving a few duplicate OnBecameXXX events during initial scrolling and don't know why
    // It does not seem to be from other cameras in the scene. Don't know about editor scene camera.
    private bool ValidateVisibilityChange(bool isVisible) {
        if (!_isGameRunning) {
            return false;   // see SetupDocs.txt for approach to visibility
        }
        bool isValid = true;
        string visibility = isVisible ? "Visible" : "Invisible";
        if (isVisible == _isVisible) {
            D.LogContext("Duplicate {0}.OnBecame{1}() received and filtered out.".Inject(gameObject.name, visibility), this);
            isValid = false;
        }
        if (gameObject.activeInHierarchy) {
            if (isVisible != renderer.IsVisibleFrom(Camera.main)) {
                D.WarnContext("{0}.OnBecame{1}() received from a camera that is not Camera.main.".Inject(gameObject.name, visibility), this);
                isValid = false;
            }
        }
        return isValid;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
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

