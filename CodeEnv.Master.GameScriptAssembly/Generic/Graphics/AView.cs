// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AView.cs
// Abstract base class managing the UI View for its AItem. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class managing the UI View for its AItem. 
/// </summary>
public abstract class AView : AMonoBase, IViewable, ICameraLOSChangedClient, IDisposable {

    private ViewDisplayMode _displayMode = ViewDisplayMode.ThreeDAnimation; // start up showing all, then sync up
    protected ViewDisplayMode DisplayMode {
        get { return _displayMode; }
        set { SetProperty<ViewDisplayMode>(ref _displayMode, value, "DisplayMode", OnDisplayModeChanged, OnDisplayModeChanging); }
    }

    private ViewDisplayMode _cameraDistanceGeneratedDisplayMode = ViewDisplayMode.ThreeDAnimation;  // start up showing all, then sync up

    private IList<Transform> _meshesInCameraLOS = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter

    protected override void Awake() {
        base.Awake();
        enabled = false;
    }

    protected virtual void OnPlayerIntelLevelChanged() {
        AssessDisplayMode();
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            ShowHud(true);
        }
    }

    protected virtual void OnInCameraLOSChanged() {
        AssessDisplayMode();
    }

    protected virtual void OnDisplayModeChanging(ViewDisplayMode newMode) { }

    protected virtual void OnDisplayModeChanged() {
        if (DisplayMode == ViewDisplayMode.Hide) {
            ShowHud(false);
        }
    }

    private void OnMeshNotifingCameraLOSChanged(Transform sender, bool inLOS) {
        if (inLOS) {
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
            if (!_meshesInCameraLOS.Contains(sender)) {
                _meshesInCameraLOS.Add(sender);
            }
        }
        else {
            _meshesInCameraLOS.Remove(sender);
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
        }

        if (InCameraLOS == (_meshesInCameraLOS.Count == 0)) {
            // visibility state of this object should now change
            InCameraLOS = !InCameraLOS;
            //D.Log("{0}.InCameraLOS changed to {1}.", gameObject.name, InCameraLOS);
        }
    }

    private void AssessDisplayMode() {
        if (!InCameraLOS || PlayerIntelLevel == IntelLevel.Nil || _cameraDistanceGeneratedDisplayMode == ViewDisplayMode.Hide) {
            DisplayMode = ViewDisplayMode.Hide;
            return;
        }
        if (InCameraLOS && PlayerIntelLevel != IntelLevel.Nil) {
            DisplayMode = _cameraDistanceGeneratedDisplayMode;
        }
    }

    public void ShowHud(bool toShow) {
        if (HudPublisher != null) {
            HudPublisher.ShowHud(toShow, PlayerIntelLevel);
            return;
        }
        D.Warn("{0} HudPublisher is null.", gameObject.name);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    protected virtual void Cleanup() {
        if (HudPublisher != null) {
            (HudPublisher as IDisposable).Dispose();
        }
    }

    #region IViewable Members

    public abstract float Radius { get; }

    private IntelLevel _playerIntelLevel;
    public virtual IntelLevel PlayerIntelLevel {
        get {
            return _playerIntelLevel;
        }
        set {
            SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnPlayerIntelLevelChanged);
        }
    }

    public IGuiHudPublisher HudPublisher { get; set; }

    public void RecordDesiredDisplayModeDerivedFromCameraDistance(ViewDisplayMode cameraDistanceGeneratedDisplayMode) {
        _cameraDistanceGeneratedDisplayMode = cameraDistanceGeneratedDisplayMode;
        AssessDisplayMode();
    }

    #endregion

    #region ICameraLOSChangedClient Members

    private bool _inCameraLOS = true; // everyone starts out thinking they are visible as it controls the enabled/activated state of key components
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        private set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnInCameraLOSChanged); }
    }

    public void NotifyCameraLOSChanged(Transform sender, bool inLOS) {
        OnMeshNotifingCameraLOSChanged(sender, inLOS);
    }

    #endregion

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

