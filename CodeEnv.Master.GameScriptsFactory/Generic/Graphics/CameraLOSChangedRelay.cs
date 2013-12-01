// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraLOSChangedRelay.cs
// Conveys changes in its Renderer's 'visibility state' (in or out of the camera's line of sight) to 
// one or more client gameobjects that implement the ICameraLOSChangedClient interface.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Conveys changes in its Renderer's 'visibility state' (in or out of the camera's line of sight) to 
/// one or more client gameobjects that implement the ICameraLOSChangedClient interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
public class CameraLOSChangedRelay : AMonoBase, IDisposable {

    public List<Transform> relayTargets;

    private IList<ICameraLOSChangedClient> _iRelayTargets;
    private bool _isRunning;
    private IList<IDisposable> _subscribers;
    private Renderer _renderer;

    protected override void Awake() {
        base.Awake();
        _renderer = UnityUtility.ValidateComponentPresence<Renderer>(gameObject);
        _renderer.enabled = true;   // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!
        relayTargets = relayTargets ?? new List<Transform>();
        if (relayTargets.Count == 0) {
            Transform relayTarget = _transform.GetSafeTransformWithInterfaceInParents<ICameraLOSChangedClient>();
            if (relayTarget != null) {
                D.Warn("{0} {1} target field is not assigned. Automatically assigning {2} as target.", _transform.name, this.GetType().Name, relayTarget.name);
            }
            else {
                D.Warn("No {0} assigned or found for {1}.", typeof(ICameraLOSChangedClient), _transform.name);
                return;
            }
            relayTargets.Add(relayTarget);
        }

        _iRelayTargets = new List<ICameraLOSChangedClient>();

        foreach (var target in relayTargets) {
            ICameraLOSChangedClient iTarget = target.GetInterface<ICameraLOSChangedClient>();
            if (iTarget == null) {
                D.Warn("{0} is not an {1}.", target.name, typeof(ICameraLOSChangedClient));
                continue;
            }
            _iRelayTargets.Add(iTarget);
        }
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameStatus.Instance.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsRunning, OnIsRunningChanged));
    }

    public void AddTarget(params Transform[] targets) {
        foreach (var target in targets) {
            // validate target is a INotify... and that it is not already present
            var iTarget = target.gameObject.GetSafeInterface<ICameraLOSChangedClient>();
            if (iTarget != null && !relayTargets.Contains(target)) {
                // add it to the lists
                relayTargets.Add(target);
                _iRelayTargets.Add(iTarget);
            }
        }
    }

    private void OnIsRunningChanged() {
        _isRunning = GameStatus.Instance.IsRunning;
        if (_isRunning) {
            // all relay targets start out initialized with IsVisible = true. This just initializes their list of child meshes that think they are visible
            OnBecameVisible();
        }
    }

    private bool _inCameraLOS;    // starts false so startup OnBecameVisible events don't generate warnings in ValidateVisibiltyChange
    void OnBecameVisible() {
        //D.Log("{0} VisibilityRelay has received OnBecameVisible().", _transform.name);
        if (ValidateCameraLOSChange(inLOS: true)) {
            for (int i = 0; i < relayTargets.Count; i++) {
                ICameraLOSChangedClient client = _iRelayTargets[i];
                if (client != null) {
                    LogCameraLOSChange(_transform.name, relayTargets[i].name, inLOS: true);
                    client.NotifyCameraLOSChanged(_transform, inLOS: true);
                }
            }
            // more efficient and easier but can't provide the client target name for debug
            //foreach (var iNotify in _iRelayTargets) {    
            //    if (iNotify != null) {
            //        iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
            //        D.Log("{0} has notified a client of becoming Visible.", _transform.name);
            //    }
            //}
            _inCameraLOS = true;
        }
    }

    void OnBecameInvisible() {
        //D.Log("{0} VisibilityRelay has received OnBecameInvisible().", _transform.name);
        if (ValidateCameraLOSChange(inLOS: false)) {
            for (int i = 0; i < relayTargets.Count; i++) {
                Transform t = relayTargets[i];
                if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                    ICameraLOSChangedClient client = _iRelayTargets[i];
                    if (client != null) {
                        LogCameraLOSChange(_transform.name, relayTargets[i].name, inLOS: false);
                        client.NotifyCameraLOSChanged(_transform, inLOS: false);
                    }
                }
            }
            _inCameraLOS = false;
        }
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    private void LogCameraLOSChange(string notifier, string client, bool inLOS) {
        if (DebugSettings.Instance.EnableVerboseDebugLog) {
            string iNotifyParentName = _transform.GetSafeTransformWithInterfaceInParents<ICameraLOSChangedClient>().name;
            string visibility = inLOS ? "InCameraLOS" : "OutOfCameraLOS";
            D.Log("{0} of parent {1} is notifying client {2} of becoming {3}.", notifier, iNotifyParentName, client, visibility);
        }
    }

    // FIXME Recieving a few duplicate OnBecameXXX events during initial scrolling and don't know why
    // It does not seem to be from other cameras in the scene. Don't know about editor scene camera.
    private bool ValidateCameraLOSChange(bool inLOS) {
        if (!_isRunning) {
            return false;   // see SetupDocs.txt for approach to visibility
        }
        bool isValid = true;
        string visibility = inLOS ? "Visible" : "Invisible";
        if (inLOS == _inCameraLOS) {
            //D.LogContext("Duplicate {0}.OnBecame{1}() received and filtered out.".Inject(gameObject.name, visibility), this);
            isValid = false;
        }
        if (gameObject.activeInHierarchy) {
            if (inLOS != renderer.InLineOfSightOf(Camera.main)) {
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

