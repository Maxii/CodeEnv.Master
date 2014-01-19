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

    private IList<Transform> _meshesInCameraLOS = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        enabled = false;
    }

    protected override void Start() {
        base.Start();
        InitializePresenter();  // moved from Awake as some Presenters need immediate access to this Behaviour's parent which may not yet be assigned if Instantiated at runtime
    }

    protected abstract void InitializePresenter();

    protected virtual void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(PlayerIntel.SubscribeToPropertyChanged<Intel, IntelSource>(pi => pi.Source, OnPlayerIntelContentChanged));
    }

    private void OnPlayerIntelChanging(Intel newIntel) {
        Unsubscribe();
    }


    private void OnPlayerIntelChanged() {
        Subscribe();
        OnPlayerIntelContentChanged();
    }

    protected virtual void OnPlayerIntelContentChanged() {
        AssessDiscernability();
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            ShowHud(true);
        }
    }

    protected virtual void OnInCameraLOSChanged() {
        AssessDiscernability();
    }

    protected virtual void OnIsDiscernibleChanged() {
        D.Log("{0}.OnIsDiscernibleChanged(), isDiscernible = {1}.", _transform.name, IsDiscernible);
        if (!IsDiscernible) {
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
            D.Log("{0}.InCameraLOS changed to {1}.", gameObject.name, !InCameraLOS);
            InCameraLOS = !InCameraLOS;
        }
    }

    protected virtual void AssessDiscernability() {
        //D.Log("{0}.{1}.AssessDiscernability() called. PlayerIntel.Source = {2}.", _transform.parent.name, _transform.name, PlayerIntel.Source.GetName());
        IsDiscernible = InCameraLOS && PlayerIntel.Source != IntelSource.None;
    }

    public void ShowHud(bool toShow) {
        if (!enabled) { return; }
        if (HudPublisher == null) {
            D.Warn("{0} HudPublisher is null.", gameObject.name);
            return;
        }
        HudPublisher.ShowHud(toShow, PlayerIntel);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    protected virtual void Cleanup() {
        if (HudPublisher != null) {
            (HudPublisher as IDisposable).Dispose();
        }
        Unsubscribe();
    }

    protected void Unsubscribe() {
        if (_subscribers != null) {
            _subscribers.ForAll(s => s.Dispose());
            _subscribers.Clear();
        }
    }

    #region IViewable Members

    public abstract float Radius { get; }

    private Intel _playerIntel;
    public Intel PlayerIntel {
        get { return _playerIntel; }
        set { SetProperty<Intel>(ref _playerIntel, value, "PlayerIntel", OnPlayerIntelChanged, OnPlayerIntelChanging); }
    }

    public IGuiHudPublisher HudPublisher { get; set; }

    private bool _isDiscernible;
    public bool IsDiscernible {
        get { return _isDiscernible; }
        set { SetProperty<bool>(ref _isDiscernible, value, "IsDiscernible", OnIsDiscernibleChanged); }
    }

    #endregion

    #region ICameraLOSChangedClient Members

    private bool _inCameraLOS;  // = true; // everyone starts out thinking they are visible as it controls the enabled/activated state of key components
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

