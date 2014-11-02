// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemView.cs
// Abstract base class managing the UI View for its AItem. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class managing the UI View for its AItem. 
/// </summary>
public abstract class AItemView : AMonoBase, IViewable, IDisposable {

    private bool _inCameraLOS = true;
    /// <summary>
    /// Indicates whether this view is within a Camera's Line Of Sight.
    /// Note: All items start out thinking they are in a camera's LOS. This is so IsDiscernible will properly operate
    /// during the period when a view's visual members have not yet been initialized. If and when they are
    /// initialized, the view will be notified by their CameraLosChangedListener of their actual InCameraLOS state.
    /// </summary>
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        protected set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnInCameraLOSChanged); }
    }

    protected IList<IDisposable> _subscribers;
    private bool _isVisualMembersInitialized;

    protected override void Awake() {
        base.Awake();
        PlayerIntel = InitializePlayerIntel();
        enabled = false;
    }

    protected override void Start() {
        base.Start();
        InitializePresenter();  // moved from Awake as some Presenters need immediate access to this Behaviour's parent which may not yet be assigned if Instantiated at runtime
        // Derived classes must call Subscribe after all references are available
    }

    protected virtual IIntel InitializePlayerIntel() { return new Intel(); }

    protected abstract void InitializePresenter();

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        SubscribeToPlayerIntelCoverageChanged();
    }

    protected virtual void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as Intel).SubscribeToPropertyChanged<Intel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    protected virtual void OnPlayerIntelCoverageChanged() {
        CustomLogEvent("Coverage = {0}".Inject(PlayerIntel.CurrentCoverage.GetName()));
        AssessDiscernability();
        if (HudPublisher.IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
    }

    protected virtual void OnInCameraLOSChanged() {
        CustomLogEvent("InCameraLOS = {0}".Inject(InCameraLOS));
        AssessDiscernability();
    }

    protected virtual void OnIsDiscernibleChanged() {
        CustomLogEvent("IsDiscernible = {0}".Inject(IsDiscernible));
        if (!IsDiscernible && HudPublisher.IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_isVisualMembersInitialized) {
            D.Assert(IsDiscernible);    // first time change should always be to true
            InitializeVisualMembers();
            _isVisualMembersInitialized = true;
        }
    }

    /// <summary>
    /// Initializes the visual members of this view.
    /// </summary>
    protected abstract void InitializeVisualMembers();

    public virtual void AssessDiscernability() {
        CustomLogEvent();
        IsDiscernible = InCameraLOS && PlayerIntel.CurrentCoverage != IntelCoverage.None;
    }

    public void ShowHud(bool toShow) {
        //if (!enabled) { return; }
        D.Log("HudPublisher.Show() called from {0}.", _transform.name);
        HudPublisher.ShowHud(toShow, PlayerIntel, _transform.position);
    }

    #region Debug

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    private void CustomLogEvent(string arg = "") { // custom for this base method as parents don't always exist
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string parentName = _transform.parent != null ? _transform.parent.name : "(no parent)";
        D.Log("{0}.{1}.{2}.{3}() called. {4}.".Inject(parentName, _transform.name, GetType().Name, stackFrame.GetMethod().Name, arg));
    }

    #endregion

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

    public IIntel PlayerIntel { get; private set; }

    public IGuiHudPublisher HudPublisher { get; set; }

    private bool _isDiscernible;
    public bool IsDiscernible {
        get { return _isDiscernible; }
        protected set { SetProperty<bool>(ref _isDiscernible, value, "IsDiscernible", OnIsDiscernibleChanged); }
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

